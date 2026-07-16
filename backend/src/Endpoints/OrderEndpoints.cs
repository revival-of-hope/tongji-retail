using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("顾客从购物车创建订单；单个订单仅允许同一商家的商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<OrderDetail>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        group.MapGet("/", GetMyOrdersAsync)
            .WithName("GetMyOrders")
            .WithSummary("获取当前顾客订单")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<IReadOnlyList<OrderSummary>>>()
            .WithStandardErrors();

        group.MapGet("/{id:long}", GetOrderAsync)
            .WithName("GetOrder")
            .WithSummary("按角色权限获取订单详情")
            .Produces<ApiEnvelope<OrderDetail>>()
            .WithStandardErrors();

        group.MapPost("/{id:long}/pay", PayOrderAsync)
            .WithName("PayOrder")
            .WithSummary("模拟支付并在事务中扣减库存")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<OrderDetail>>()
            .WithStandardErrors();

        group.MapPut("/{id:long}/ship", ShipOrderAsync)
            .WithName("ShipOrder")
            .WithSummary("商家对自己的待发货订单执行发货")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<OrderDetail>>()
            .WithStandardErrors();

        group.MapPut("/{id:long}/complete", CompleteOrderAsync)
            .WithName("CompleteOrder")
            .WithSummary("顾客确认收货")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<OrderDetail>>()
            .WithStandardErrors();

        group.MapPut("/{id:long}/cancel", CancelOrderAsync)
            .WithName("CancelOrder")
            .WithSummary("顾客取消未发货订单；已支付时恢复库存")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<OrderDetail>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> CreateOrderAsync(CreateOrderRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (request.CartItemIds.Count == 0) return ApiResults.BadRequest("至少选择一件购物车商品");
        if (string.IsNullOrWhiteSpace(request.ShippingAddress) || request.ShippingAddress.Trim().Length > 500)
            return ApiResults.BadRequest("收货地址不能为空且不能超过 500 个字符");
        if (request.Remark?.Length > 500) return ApiResults.BadRequest("订单备注不能超过 500 个字符");

        var userId = principal.GetUserId();
        var ids = request.CartItemIds.Distinct().ToArray();
        var cartItems = await db.CartItems
            .Include(x => x.Cart)
            .Include(x => x.Product).ThenInclude(x => x.Merchant)
            .Where(x => ids.Contains(x.Id) && x.Cart.UserId == userId)
            .ToListAsync(cancellationToken);
        if (cartItems.Count != ids.Length) return ApiResults.BadRequest("购物车商品不存在或不属于当前用户");
        if (cartItems.Select(x => x.Product.MerchantId).Distinct().Count() != 1)
            return ApiResults.BadRequest("一次结算只能选择同一商家的商品，请分开下单");
        if (cartItems.Any(x => x.Product.Status != ProductStatus.OnSale || x.Quantity > x.Product.StockQuantity))
            return ApiResults.BadRequest("部分商品已下架或库存不足");

        var order = new Order
        {
            UserId = userId,
            OrderNo = GenerateOrderNo(),
            ShippingAddress = request.ShippingAddress.Trim(),
            Remark = request.Remark?.Trim(),
            ExpireAt = DateTime.UtcNow.AddMinutes(30),
            Items = cartItems.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                UnitPrice = x.Product.Price,
                SubTotal = x.Product.Price * x.Quantity
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(x => x.SubTotal);

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cartItems);
        var cart = cartItems[0].Cart;
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        order = await OrderQuery(db).SingleAsync(x => x.Id == order.Id, cancellationToken);
        return ApiResults.Created($"/api/orders/{order.Id}", order.ToDetail(), "订单创建成功，请在 30 分钟内支付");
    }

    private static async Task<IResult> GetMyOrdersAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var orders = await OrderQuery(db).AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<OrderSummary>>(orders.Select(x => x.ToSummary()).ToArray());
    }

    private static async Task<IResult> GetOrderAsync(long id, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var order = await OrderQuery(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return ApiResults.NotFound("订单不存在");
        var userId = principal.GetUserId();
        var role = principal.GetRoleName();
        var allowed = role == nameof(UserRole.Admin)
            || (role == nameof(UserRole.Customer) && order.UserId == userId)
            || (role == nameof(UserRole.Merchant) && order.Items.All(x => x.Product.Merchant.UserId == userId));
        return allowed ? ApiResults.Ok(order.ToDetail()) : ApiResults.Forbidden("无权查看该订单");
    }

    private static async Task<IResult> PayOrderAsync(long id, PayOrderRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var userId = principal.GetUserId();
            var order = await OrderQuery(db).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
            if (order is null) return ApiResults.NotFound("订单不存在");
            if (order.Status != OrderStatus.PendingPayment) return ApiResults.Conflict("订单当前状态不可支付");
            if (order.ExpireAt <= DateTime.UtcNow)
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return ApiResults.Conflict("订单已超过支付期限并自动取消");
            }
            if (order.Items.Any(x => x.Product.Status != ProductStatus.OnSale || x.Product.StockQuantity < x.Quantity))
                return ApiResults.Conflict("商品库存不足或已下架，支付失败");

            foreach (var item in order.Items)
            {
                item.Product.StockQuantity -= item.Quantity;
                item.Product.SoldCount += item.Quantity;
                item.Product.UpdatedAt = DateTime.UtcNow;
            }

            order.Status = OrderStatus.PendingShipment;
            order.UpdatedAt = DateTime.UtcNow;
            order.Payment = new Payment
            {
                Amount = order.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                Status = PaymentStatus.Success,
                TransactionId = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..35],
                PaidAt = DateTime.UtcNow
            };
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return ApiResults.Ok(order.ToDetail(), "支付成功");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static async Task<IResult> ShipOrderAsync(long id, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var order = await OrderQuery(db).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return ApiResults.NotFound("订单不存在");
        if (order.Items.Any(x => x.Product.Merchant.UserId != userId)) return ApiResults.Forbidden("只能处理本店订单");
        if (order.Status != OrderStatus.PendingShipment) return ApiResults.Conflict("订单当前状态不可发货");
        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(order.ToDetail(), "订单已发货");
    }

    private static async Task<IResult> CompleteOrderAsync(long id, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var order = await OrderQuery(db).SingleOrDefaultAsync(x => x.Id == id && x.UserId == principal.GetUserId(), cancellationToken);
        if (order is null) return ApiResults.NotFound("订单不存在");
        if (order.Status != OrderStatus.Shipped) return ApiResults.Conflict("只有已发货订单可以确认收货");
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(order.ToDetail(), "订单已完成");
    }

    private static async Task<IResult> CancelOrderAsync(long id, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (db.Database.IsRelational()) transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var order = await OrderQuery(db).SingleOrDefaultAsync(x => x.Id == id && x.UserId == principal.GetUserId(), cancellationToken);
            if (order is null) return ApiResults.NotFound("订单不存在");
            if (order.Status is not (OrderStatus.PendingPayment or OrderStatus.PendingShipment))
                return ApiResults.Conflict("只有待支付或待发货订单可以取消");

            if (order.Status == OrderStatus.PendingShipment)
            {
                foreach (var item in order.Items)
                {
                    item.Product.StockQuantity += item.Quantity;
                    item.Product.SoldCount = Math.Max(0, item.Product.SoldCount - item.Quantity);
                    item.Product.UpdatedAt = DateTime.UtcNow;
                }
                if (order.Payment is not null) order.Payment.Status = PaymentStatus.Refunded;
            }
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return ApiResults.Ok(order.ToDetail(), "订单已取消");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    internal static IQueryable<Order> OrderQuery(AppDbContext db) => db.Orders
        .Include(x => x.User)
        .Include(x => x.Payment)
        .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Merchant)
        .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Images);

    private static string GenerateOrderNo() => $"RS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
}
