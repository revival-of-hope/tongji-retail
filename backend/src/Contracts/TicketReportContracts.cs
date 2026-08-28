using RetailSystem.Api.Models;

namespace RetailSystem.Api.Contracts;

public sealed record CreateTicketRequest(long? OrderId, string Subject, string Description);
public sealed record ReplyTicketRequest(string Reply, TicketStatus Status);

public sealed record TicketResponse(
    long Id,
    long UserId,
    string Username,
    long? OrderId,
    long? AssignedTo,
    string? AssignedUsername,
    string Subject,
    string Description,
    string? Reply,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OverviewReport(
    decimal TotalSales,
    int TotalOrders,
    int TotalUsers,
    int TotalProducts,
    int PendingProducts,
    int PendingMerchants,
    int OpenTickets);

public sealed record DailySalesPoint(DateOnly Date, decimal Sales, int Orders);
public sealed record CategorySalesPoint(long CategoryId, string CategoryName, decimal Sales, int Quantity);
public sealed record ProductSalesPoint(long ProductId, string ProductName, int Quantity, decimal Sales);

public sealed record MerchantReport(
    decimal TotalSales,
    int TotalOrders,
    IReadOnlyList<DailySalesPoint> DailySales,
    IReadOnlyList<ProductSalesPoint> TopProducts);
