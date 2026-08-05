using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Services;

public static class OrderMaintenance
{
    public static async Task<int> ExpirePendingOrdersAsync(
        AppDbContext db,
        CancellationToken cancellationToken,
        long? userId = null,
        long? orderId = null)
    {
        var now = DateTime.UtcNow;
        var query = db.Orders.Where(order =>
            order.Status == OrderStatus.PendingPayment && order.ExpireAt <= now);

        if (userId.HasValue) query = query.Where(order => order.UserId == userId.Value);
        if (orderId.HasValue) query = query.Where(order => order.Id == orderId.Value);

        var expiredOrders = await query.ToListAsync(cancellationToken);
        if (expiredOrders.Count == 0) return 0;

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return expiredOrders.Count;
    }
}

public sealed class ExpiredOrderCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredOrderCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var count = await OrderMaintenance.ExpirePendingOrdersAsync(db, stoppingToken);
                if (count > 0) logger.LogInformation("Automatically cancelled {Count} expired orders", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clean up expired orders");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
