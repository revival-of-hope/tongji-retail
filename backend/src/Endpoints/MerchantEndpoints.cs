using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class MerchantEndpoints
{
    public static IEndpointRouteBuilder MapMerchantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/merchants").WithTags("Merchants").RequireAuthorization();

        group.MapPost("/apply", ApplyAsync)
            .WithName("ApplyMerchant")
            .WithSummary("顾客申请成为商家")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<MerchantSummary>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        group.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingMerchants")
            .WithSummary("管理员获取待审核商家")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<IReadOnlyList<MerchantSummary>>>()
            .WithStandardErrors();

        group.MapPut("/{id:long}/review", ReviewAsync)
            .WithName("ReviewMerchant")
            .WithSummary("管理员审核商家申请")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<MerchantSummary>>()
            .WithStandardErrors();

        group.MapGet("/me", GetMineAsync)
            .WithName("GetMyMerchant")
            .WithSummary("获取当前用户商家申请或店铺信息")
            .Produces<ApiEnvelope<MerchantSummary>>()
            .WithStandardErrors();

        group.MapGet("/my-products", GetMyProductsAsync)
            .WithName("GetMerchantProducts")
            .WithSummary("商家获取自己的全部商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<IReadOnlyList<ProductListItem>>>()
            .WithStandardErrors();

        group.MapGet("/my-orders", GetMyOrdersAsync)
            .WithName("GetMerchantOrders")
            .WithSummary("商家获取本店订单")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<IReadOnlyList<OrderSummary>>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> ApplyAsync(ApplyMerchantRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StoreName) || request.StoreName.Trim().Length > 100)
            return ApiResults.BadRequest("店铺名称不能为空且不能超过 100 个字符");
        if (request.Description?.Length > 500) return ApiResults.BadRequest("店铺描述不能超过 500 个字符");

        var userId = principal.GetUserId();
        var merchant = await db.Merchants.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (merchant is not null && merchant.Status is MerchantStatus.Pending or MerchantStatus.Approved)
            return ApiResults.Conflict("已有待审核或已通过的商家申请");

        if (merchant is null)
        {
            merchant = new Merchant
            {
                UserId = userId,
                StoreName = request.StoreName.Trim(),
                Description = request.Description?.Trim(),
                Status = MerchantStatus.Pending
            };
            db.Merchants.Add(merchant);
        }
        else
        {
            merchant.StoreName = request.StoreName.Trim();
            merchant.Description = request.Description?.Trim();
            merchant.Status = MerchantStatus.Pending;
            merchant.CreatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Created($"/api/merchants/{merchant.Id}", merchant.ToSummary(), "商家申请已提交");
    }

    private static async Task<IResult> GetPendingAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var merchants = await db.Merchants.AsNoTracking().Where(x => x.Status == MerchantStatus.Pending)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<MerchantSummary>>(merchants.Select(x => x.ToSummary()).ToArray());
    }

    private static async Task<IResult> ReviewAsync(long id, ReviewMerchantRequest request, AppDbContext db, CancellationToken cancellationToken)
    {
        var merchant = await db.Merchants.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (merchant is null) return ApiResults.NotFound("商家申请不存在");
        if (merchant.Status != MerchantStatus.Pending) return ApiResults.Conflict("该商家申请已完成审核");
        merchant.Status = request.Approved ? MerchantStatus.Approved : MerchantStatus.Rejected;
        merchant.User.Role = request.Approved ? UserRole.Merchant : UserRole.Customer;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(merchant.ToSummary(), request.Approved ? "商家审核通过" : "商家审核拒绝");
    }

    private static async Task<IResult> GetMineAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var merchant = await db.Merchants.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == principal.GetUserId(), cancellationToken);
        return merchant is null ? ApiResults.NotFound("尚未提交商家申请") : ApiResults.Ok(merchant.ToSummary());
    }

    private static async Task<IResult> GetMyProductsAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var products = await db.Products.AsNoTracking()
            .Include(x => x.Merchant).Include(x => x.Category).Include(x => x.Images)
            .Where(x => x.Merchant.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<ProductListItem>>(products.Select(x => x.ToListItem()).ToArray());
    }

    private static async Task<IResult> GetMyOrdersAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var orders = await OrderEndpoints.OrderQuery(db).AsNoTracking()
            .Where(x => x.Items.All(i => i.Product.Merchant.UserId == userId))
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<OrderSummary>>(orders.Select(x => x.ToSummary()).ToArray());
    }
}
