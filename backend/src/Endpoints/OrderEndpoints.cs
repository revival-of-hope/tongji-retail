using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
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

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        ClaimsPrincipal principal,
        SerializableTransactionExecutor transactions,
        CancellationToken cancellationToken)
    {
        var validationError = OrderValidation.Create(
            request.CartItemIds,
            request.ShippingAddress,
            request.Remark);

        if (validationError is not null)
            return ApiResults.BadRequest(validationError);

        var ids = request.CartItemIds.Distinct().ToArray();

        return await transactions.ExecuteAsync(async (db, ct) =>
        {
            var userId = principal.GetUserId();
            var cartItems = await db.CartItems
                .Include(item => item.Cart)
                .Include(item => item.Product).ThenInclude(product => product.Merchant)
                .Where(item => ids.Contains(item.Id) && item.Cart.UserId == userId)
                .ToListAsync(ct);
            if (cartItems.Count != ids.Length) return ApiResults.BadRequest("购物车商品不存在或不属于当前用户");
            if (cartItems.Select(item => item.Product.MerchantId).Distinct().Count() != 1)
                return ApiResults.BadRequest("一次结算只能选择同一商家的商品，请分开下单");
            if (cartItems.Any(item =>
                    item.Quantity <= 0 ||
                    item.Product.Status != ProductStatus.OnSale ||
                    item.Quantity > item.Product.StockQuantity))
                return ApiResults.BadRequest("部分商品已下架、数量无效或库存不足");

            var order = new Order
            {
                UserId = userId,
                OrderNo = GenerateOrderNo(),
                ShippingAddress = request.ShippingAddress.Trim(),
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                ExpireAt = DateTime.UtcNow.AddMinutes(30),
                Items = cartItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                    SubTotal = item.Product.Price * item.Quantity
                }).ToList()
            };
            order.TotalAmount = order.Items.Sum(item => item.SubTotal);
            if (order.TotalAmount <= 0 || order.TotalAmount > Validation.MaxMoney)
                return ApiResults.BadRequest("订单金额超出有效范围");

            db.Orders.Add(order);
            db.CartItems.RemoveRange(cartItems);
            cartItems[0].Cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            order = await OrderQuery(db).SingleAsync(item => item.Id == order.Id, ct);
            return ApiResults.Created(
                $"/api/orders/{order.Id}",
                order.ToDetail(),
                "订单创建成功，请在 30 分钟内支付");
        }, cancellationToken);
    }

    private static async Task<IResult> GetMyOrdersAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        await OrderMaintenance.ExpirePendingOrdersAsync(db, cancellationToken, userId: userId);

        var orders = await OrderQuery(db)
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<OrderSummary>>(
            orders.Select(order => order.ToSummary()).ToArray());
    }

    private static async Task<IResult> GetOrderAsync(
        long id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        await OrderMaintenance.ExpirePendingOrdersAsync(db, cancellationToken, orderId: id);
        var order = await OrderQuery(db)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null) return ApiResults.NotFound("订单不存在");

        var userId = principal.GetUserId();
        var role = principal.GetRoleName();
        var allowed = role == nameof(UserRole.Admin)
            || (role == nameof(UserRole.Customer) && order.UserId == userId)
            || (role == nameof(UserRole.Merchant) && order.Items.All(item => item.Product.Merchant.UserId == userId));
        return allowed ? ApiResults.Ok(order.ToDetail()) : ApiResults.Forbidden("无权查看该订单");
    }

    private static async Task<IResult> PayOrderAsync(
        long id,
        PayOrderRequest request,
        ClaimsPrincipal principal,
        SerializableTransactionExecutor transactions,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.PaymentMethod)) return ApiResults.BadRequest("支付方式无效");

        return await transactions.ExecuteAsync(async (db, ct) =>
        {
            var userId = principal.GetUserId();
            var order = await OrderQuery(db)
                .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, ct);
            if (order is null) return ApiResults.NotFound("订单不存在");
            if (order.Status != OrderStatus.PendingPayment) return ApiResults.Conflict("订单当前状态不可支付");
            if (order.ExpireAt <= DateTime.UtcNow)
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return ApiResults.Conflict("订单已超过支付期限并自动取消");
            }

            if (order.Items.Any(item =>
                    item.Product.Status != ProductStatus.OnSale ||
                    item.Product.StockQuantity < item.Quantity))
                return ApiResults.Conflict("商品库存不足或已下架，支付失败");
            if (order.Items.Any(item => item.Product.SoldCount > int.MaxValue - item.Quantity))
                return ApiResults.Conflict("商品销量计数已达到系统上限，请联系管理员处理");

            var now = DateTime.UtcNow;
            foreach (var item in order.Items)
            {
                item.Product.StockQuantity -= item.Quantity;
                item.Product.SoldCount += item.Quantity;
                item.Product.UpdatedAt = now;
            }

            order.Status = OrderStatus.PendingShipment;
            order.UpdatedAt = now;
            order.Payment = new Payment
            {
                Amount = order.TotalAmount,
                PaymentMethod = request.PaymentMethod,
                Status = PaymentStatus.Success,
                TransactionId = $"SIM-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..50],
                PaidAt = now
            };
            await db.SaveChangesAsync(ct);
            return ApiResults.Ok(order.ToDetail(), "支付成功");
        }, cancellationToken);
    }

    private static async Task<IResult> ShipOrderAsync(
        long id,
        ClaimsPrincipal principal,
        SerializableTransactionExecutor transactions,
        CancellationToken cancellationToken)
    {
        return await transactions.ExecuteAsync(async (db, ct) =>
        {
            var userId = principal.GetUserId();
            var order = await OrderQuery(db).SingleOrDefaultAsync(item => item.Id == id, ct);
            if (order is null) return ApiResults.NotFound("订单不存在");
            if (order.Items.Any(item => item.Product.Merchant.UserId != userId))
                return ApiResults.Forbidden("只能处理本店订单");
            if (order.Status != OrderStatus.PendingShipment) return ApiResults.Conflict("订单当前状态不可发货");

            order.Status = OrderStatus.Shipped;
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return ApiResults.Ok(order.ToDetail(), "订单已发货");
        }, cancellationToken);
    }

    private static async Task<IResult> CompleteOrderAsync(
        long id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var order = await OrderQuery(db).SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == principal.GetUserId(),
            cancellationToken);
        if (order is null) return ApiResults.NotFound("订单不存在");
        if (order.Status != OrderStatus.Shipped) return ApiResults.Conflict("只有已发货订单可以确认收货");

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(order.ToDetail(), "订单已完成");
    }

    private static async Task<IResult> CancelOrderAsync(
        long id,
        ClaimsPrincipal principal,
        SerializableTransactionExecutor transactions,
        CancellationToken cancellationToken)
    {
        return await transactions.ExecuteAsync(async (db, ct) =>
        {
            var order = await OrderQuery(db).SingleOrDefaultAsync(
                item => item.Id == id && item.UserId == principal.GetUserId(),
                ct);
            if (order is null) return ApiResults.NotFound("订单不存在");
            if (order.Status is not (OrderStatus.PendingPayment or OrderStatus.PendingShipment))
                return ApiResults.Conflict("只有待支付或待发货订单可以取消");

            if (order.Status == OrderStatus.PendingShipment)
            {
                if (order.Items.Any(item => (long)item.Product.StockQuantity + item.Quantity > int.MaxValue))
                    return ApiResults.Conflict("库存恢复超出允许范围，请联系管理员处理");

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
            await db.SaveChangesAsync(ct);
            return ApiResults.Ok(order.ToDetail(), "订单已取消");
        }, cancellationToken);
    }

    internal static IQueryable<Order> OrderQuery(AppDbContext db) => db.Orders
        .Include(order => order.User)
        .Include(order => order.Payment)
        .Include(order => order.Reviews)
        .Include(order => order.Items).ThenInclude(item => item.Product).ThenInclude(product => product.Merchant)
        .Include(order => order.Items).ThenInclude(item => item.Product).ThenInclude(product => product.Images);

    private static string GenerateOrderNo()
    {
        var value = $"RS{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}";
        return value[..48];
    }
}
