using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart")
            .WithTags("Cart")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)));

        group.MapGet("/", GetCartAsync)
            .WithName("GetCart")
            .WithSummary("获取当前顾客购物车")
            .Produces<ApiEnvelope<IReadOnlyList<CartItemResponse>>>()
            .WithStandardErrors();

        group.MapPost("/items", AddItemAsync)
            .WithName("AddCartItem")
            .WithSummary("添加商品到购物车")
            .Produces<ApiEnvelope<CartItemResponse>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        group.MapPut("/items/{cartItemId:long}", UpdateItemAsync)
            .WithName("UpdateCartItem")
            .WithSummary("修改购物车商品数量")
            .Produces<ApiEnvelope<CartItemResponse>>()
            .WithStandardErrors();

        group.MapDelete("/items/{cartItemId:long}", DeleteItemAsync)
            .WithName("DeleteCartItem")
            .WithSummary("删除购物车商品")
            .Produces<ApiEnvelope<object>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> GetCartAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCartAsync(principal.GetUserId(), db, cancellationToken);
        var items = await CartItemsQuery(db).Where(x => x.CartId == cart.Id).OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<CartItemResponse>>(items.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> AddItemAsync(AddCartItemRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) return ApiResults.BadRequest("加购数量必须大于 0");
        var product = await db.Products.Include(x => x.Merchant).SingleOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null || product.Status != ProductStatus.OnSale) return ApiResults.NotFound("商品不存在或未上架");
        if (request.Quantity > product.StockQuantity) return ApiResults.BadRequest("库存不足");

        var cart = await GetOrCreateCartAsync(principal.GetUserId(), db, cancellationToken);
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == request.ProductId, cancellationToken);
        if (item is null)
        {
            item = new CartItem { CartId = cart.Id, ProductId = request.ProductId, Quantity = request.Quantity };
            db.CartItems.Add(item);
        }
        else
        {
            if (item.Quantity + request.Quantity > product.StockQuantity) return ApiResults.BadRequest("购物车数量超过库存");
            item.Quantity += request.Quantity;
        }
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        item = await CartItemsQuery(db).SingleAsync(x => x.Id == item.Id, cancellationToken);
        return ApiResults.Created($"/api/cart/items/{item.Id}", item.ToResponse(), "已加入购物车");
    }

    private static async Task<IResult> UpdateItemAsync(long cartItemId, UpdateCartItemRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) return ApiResults.BadRequest("数量必须大于 0；删除商品请使用 DELETE 接口");
        var userId = principal.GetUserId();
        var item = await CartItemsQuery(db).SingleOrDefaultAsync(x => x.Id == cartItemId && x.Cart.UserId == userId, cancellationToken);
        if (item is null) return ApiResults.NotFound("购物车商品不存在");
        if (item.Product.Status != ProductStatus.OnSale || request.Quantity > item.Product.StockQuantity) return ApiResults.BadRequest("商品不可购买或库存不足");

        item.Quantity = request.Quantity;
        item.Cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(item.ToResponse(), "购物车已更新");
    }

    private static async Task<IResult> DeleteItemAsync(long cartItemId, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var item = await db.CartItems.Include(x => x.Cart).SingleOrDefaultAsync(x => x.Id == cartItemId && x.Cart.UserId == userId, cancellationToken);
        if (item is null) return ApiResults.NotFound("购物车商品不存在");
        db.CartItems.Remove(item);
        item.Cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok<object>(new { itemId = cartItemId }, "已从购物车删除");
    }

    private static async Task<ShoppingCart> GetOrCreateCartAsync(long userId, AppDbContext db, CancellationToken cancellationToken)
    {
        var cart = await db.ShoppingCarts.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (cart is not null) return cart;
        cart = new ShoppingCart { UserId = userId };
        db.ShoppingCarts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private static IQueryable<CartItem> CartItemsQuery(AppDbContext db) => db.CartItems
        .Include(x => x.Cart)
        .Include(x => x.Product).ThenInclude(x => x.Merchant)
        .Include(x => x.Product).ThenInclude(x => x.Images);
}
