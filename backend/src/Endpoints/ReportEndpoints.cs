using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();

        group.MapGet("/overview", GetOverviewAsync)
            .WithName("GetOverviewReport")
            .WithSummary("管理员获取平台指标概览")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<OverviewReport>>()
            .WithStandardErrors();

        group.MapGet("/daily-sales", GetDailySalesAsync)
            .WithName("GetDailySalesReport")
            .WithSummary("管理员获取近 30 天每日销售额")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<IReadOnlyList<DailySalesPoint>>>()
            .WithStandardErrors();

        group.MapGet("/category-sales", GetCategorySalesAsync)
            .WithName("GetCategorySalesReport")
            .WithSummary("管理员获取各分类销售额")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<IReadOnlyList<CategorySalesPoint>>>()
            .WithStandardErrors();

        group.MapGet("/merchant", GetMerchantReportAsync)
            .WithName("GetMerchantReport")
            .WithSummary("商家获取本店销售报告")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<MerchantReport>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> GetOverviewAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var paidStatuses = new[] { OrderStatus.PendingShipment, OrderStatus.Shipped, OrderStatus.Completed };
        var report = new OverviewReport(
            await db.Orders.Where(x => paidStatuses.Contains(x.Status)).SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0,
            await db.Orders.CountAsync(cancellationToken),
            await db.Users.CountAsync(cancellationToken),
            await db.Products.CountAsync(cancellationToken),
            await db.Products.CountAsync(x => x.Status == ProductStatus.PendingReview, cancellationToken),
            await db.Merchants.CountAsync(x => x.Status == MerchantStatus.Pending, cancellationToken),
            await db.CustomerServiceTickets.CountAsync(x => x.Status == TicketStatus.Pending || x.Status == TicketStatus.Processing, cancellationToken));
        return ApiResults.Ok(report);
    }

    private static async Task<IResult> GetDailySalesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-29);
        var rows = await db.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= cutoff && x.Status != OrderStatus.PendingPayment && x.Status != OrderStatus.Cancelled)
            .Select(x => new { x.CreatedAt, x.TotalAmount }).ToListAsync(cancellationToken);
        var grouped = rows.GroupBy(x => DateOnly.FromDateTime(x.CreatedAt.Date))
            .ToDictionary(g => g.Key, g => new { Sales = g.Sum(x => x.TotalAmount), Orders = g.Count() });
        var result = Enumerable.Range(0, 30).Select(offset => DateOnly.FromDateTime(cutoff.AddDays(offset)))
            .Select(date => grouped.TryGetValue(date, out var value)
                ? new DailySalesPoint(date, value.Sales, value.Orders)
                : new DailySalesPoint(date, 0, 0)).ToArray();
        return ApiResults.Ok<IReadOnlyList<DailySalesPoint>>(result);
    }

    private static async Task<IResult> GetCategorySalesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.OrderItems.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Product).ThenInclude(x => x.Category)
            .Where(x => x.Order.Status != OrderStatus.PendingPayment && x.Order.Status != OrderStatus.Cancelled)
            .Select(x => new { x.Product.CategoryId, CategoryName = x.Product.Category.Name, x.Quantity, x.SubTotal })
            .ToListAsync(cancellationToken);
        var result = rows.GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new CategorySalesPoint(g.Key.CategoryId, g.Key.CategoryName, g.Sum(x => x.SubTotal), g.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.Sales).ToArray();
        return ApiResults.Ok<IReadOnlyList<CategorySalesPoint>>(result);
    }

    private static async Task<IResult> GetMerchantReportAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var cutoff = DateTime.UtcNow.Date.AddDays(-29);
        var rows = await db.OrderItems.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Product).ThenInclude(x => x.Merchant)
            .Where(x => x.Product.Merchant.UserId == userId && x.Order.Status != OrderStatus.PendingPayment && x.Order.Status != OrderStatus.Cancelled)
            .Select(x => new { x.OrderId, x.Order.CreatedAt, x.ProductId, ProductName = x.Product.Name, x.Quantity, x.SubTotal })
            .ToListAsync(cancellationToken);

        var dailyMap = rows.Where(x => x.CreatedAt >= cutoff).GroupBy(x => DateOnly.FromDateTime(x.CreatedAt.Date))
            .ToDictionary(g => g.Key, g => new { Sales = g.Sum(x => x.SubTotal), Orders = g.Select(x => x.OrderId).Distinct().Count() });
        var daily = Enumerable.Range(0, 30).Select(offset => DateOnly.FromDateTime(cutoff.AddDays(offset)))
            .Select(date => dailyMap.TryGetValue(date, out var value)
                ? new DailySalesPoint(date, value.Sales, value.Orders)
                : new DailySalesPoint(date, 0, 0)).ToArray();
        var top = rows.GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new ProductSalesPoint(g.Key.ProductId, g.Key.ProductName, g.Sum(x => x.Quantity), g.Sum(x => x.SubTotal)))
            .OrderByDescending(x => x.Quantity).Take(10).ToArray();
        var report = new MerchantReport(rows.Sum(x => x.SubTotal), rows.Select(x => x.OrderId).Distinct().Count(), daily, top);
        return ApiResults.Ok(report);
    }
}
