using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)));

        group.MapGet("/users", GetUsersAsync)
            .WithName("GetAdminUsers")
            .WithSummary("管理员获取用户列表")
            .Produces<ApiEnvelope<IReadOnlyList<UserSummary>>>()
            .WithStandardErrors();

        group.MapGet("/orders", GetOrdersAsync)
            .WithName("GetAdminOrders")
            .WithSummary("管理员获取全平台订单")
            .Produces<ApiEnvelope<IReadOnlyList<OrderSummary>>>()
            .WithStandardErrors();

        group.MapGet("/products/pending", GetPendingProductsAsync)
            .WithName("GetPendingProducts")
            .WithSummary("管理员获取待审核商品")
            .Produces<ApiEnvelope<IReadOnlyList<ProductListItem>>>()
            .WithStandardErrors();

        group.MapGet("/tickets", GetTicketsAsync)
            .WithName("GetAllTickets")
            .WithSummary("管理员获取全部客服工单")
            .Produces<ApiEnvelope<IReadOnlyList<TicketResponse>>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> GetUsersAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Merchant).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<UserSummary>>(users.Select(x => x.ToSummary()).ToArray());
    }

    private static async Task<IResult> GetOrdersAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var orders = await OrderEndpoints.OrderQuery(db).AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<OrderSummary>>(orders.Select(x => x.ToSummary()).ToArray());
    }

    private static async Task<IResult> GetPendingProductsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var products = await db.Products.AsNoTracking().Include(x => x.Merchant).Include(x => x.Category).Include(x => x.Images)
            .Where(x => x.Status == ProductStatus.PendingReview).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<ProductListItem>>(products.Select(x => x.ToListItem()).ToArray());
    }

    private static async Task<IResult> GetTicketsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var tickets = await TicketEndpoints.TicketQuery(db).AsNoTracking().OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<TicketResponse>>(tickets.Select(x => x.ToResponse()).ToArray());
    }
}
