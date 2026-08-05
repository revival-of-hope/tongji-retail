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
        await OrderMaintenance.ExpirePendingOrdersAsync(db, cancellationToken);

        var report = new OverviewReport(
            await db.Payments
                .Where(payment => payment.Status == PaymentStatus.Success)
                .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0,
            await db.Orders.CountAsync(cancellationToken),
            await db.Users.CountAsync(cancellationToken),
            await db.Products.CountAsync(cancellationToken),
            await db.Products.CountAsync(product => product.Status == ProductStatus.PendingReview, cancellationToken),
            await db.Merchants.CountAsync(merchant => merchant.Status == MerchantStatus.Pending, cancellationToken),
            await db.CustomerServiceTickets.CountAsync(
                ticket => ticket.Status == TicketStatus.Pending || ticket.Status == TicketStatus.Processing,
                cancellationToken));
        return ApiResults.Ok(report);
    }

    private static async Task<IResult> GetDailySalesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-29);
        var rows = await db.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status == PaymentStatus.Success &&
                payment.PaidAt.HasValue &&
                payment.PaidAt.Value >= cutoff)
            .Select(payment => new
            {
                PaidAt = payment.PaidAt!.Value,
                payment.Amount
            })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(row => DateOnly.FromDateTime(row.PaidAt.Date))
            .ToDictionary(
                group => group.Key,
                group => new { Sales = group.Sum(row => row.Amount), Orders = group.Count() });
        var result = Enumerable.Range(0, 30)
            .Select(offset => DateOnly.FromDateTime(cutoff.AddDays(offset)))
            .Select(date => grouped.TryGetValue(date, out var value)
                ? new DailySalesPoint(date, value.Sales, value.Orders)
                : new DailySalesPoint(date, 0, 0))
            .ToArray();
        return ApiResults.Ok<IReadOnlyList<DailySalesPoint>>(result);
    }

    private static async Task<IResult> GetCategorySalesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.Payment != null && item.Order.Payment.Status == PaymentStatus.Success)
            .GroupBy(item => new { item.Product.CategoryId, CategoryName = item.Product.Category.Name })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.CategoryName,
                Sales = group.Sum(item => item.SubTotal),
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.Sales)
            .ToListAsync(cancellationToken);
        var result = rows
            .Select(row => new CategorySalesPoint(row.CategoryId, row.CategoryName, row.Sales, row.Quantity))
            .ToArray();
        return ApiResults.Ok<IReadOnlyList<CategorySalesPoint>>(result);
    }

    private static async Task<IResult> GetMerchantReportAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var cutoff = DateTime.UtcNow.Date.AddDays(-29);
        var source = db.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Product.Merchant.UserId == userId &&
                item.Order.Payment != null &&
                item.Order.Payment.Status == PaymentStatus.Success);

        var totalSales = await source.SumAsync(item => (decimal?)item.SubTotal, cancellationToken) ?? 0;
        var totalOrders = await source
            .Select(item => item.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);
        var topProductRows = await source
            .GroupBy(item => new { item.ProductId, ProductName = item.Product.Name })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.ProductName,
                Quantity = group.Sum(item => item.Quantity),
                Sales = group.Sum(item => item.SubTotal)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.Sales)
            .Take(10)
            .ToListAsync(cancellationToken);
        var topProducts = topProductRows
            .Select(row => new ProductSalesPoint(row.ProductId, row.ProductName, row.Quantity, row.Sales))
            .ToArray();

        var dailyRows = await source
            .Where(item => item.Order.Payment!.PaidAt.HasValue && item.Order.Payment.PaidAt.Value >= cutoff)
            .Select(item => new
            {
                item.OrderId,
                PaidAt = item.Order.Payment!.PaidAt!.Value,
                item.SubTotal
            })
            .ToListAsync(cancellationToken);
        var dailyMap = dailyRows
            .GroupBy(row => DateOnly.FromDateTime(row.PaidAt.Date))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Sales = group.Sum(row => row.SubTotal),
                    Orders = group.Select(row => row.OrderId).Distinct().Count()
                });
        var dailySales = Enumerable.Range(0, 30)
            .Select(offset => DateOnly.FromDateTime(cutoff.AddDays(offset)))
            .Select(date => dailyMap.TryGetValue(date, out var value)
                ? new DailySalesPoint(date, value.Sales, value.Orders)
                : new DailySalesPoint(date, 0, 0))
            .ToArray();

        return ApiResults.Ok(new MerchantReport(totalSales, totalOrders, dailySales, topProducts));
    }
}
