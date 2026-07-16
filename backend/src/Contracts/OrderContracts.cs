using RetailSystem.Api.Models;

namespace RetailSystem.Api.Contracts;

public sealed record AddCartItemRequest(long ProductId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);

public sealed record CartItemResponse(
    long CartItemId,
    long ProductId,
    string ProductName,
    string StoreName,
    decimal UnitPrice,
    int Quantity,
    int StockQuantity,
    string? MainImageUrl,
    bool Available);

public sealed record CreateOrderRequest(
    IReadOnlyList<long> CartItemIds,
    string ShippingAddress,
    string? Remark);

public sealed record PayOrderRequest(PaymentMethod PaymentMethod);

public sealed record OrderItemResponse(
    long Id,
    long ProductId,
    string ProductName,
    string StoreName,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal,
    string? MainImageUrl);

public sealed record PaymentResponse(
    long Id,
    decimal Amount,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    string? TransactionId,
    DateTime? PaidAt);

public sealed record OrderSummary(
    long Id,
    string OrderNo,
    decimal TotalAmount,
    OrderStatus Status,
    string ShippingAddress,
    DateTime ExpireAt,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderDetail(
    long Id,
    string OrderNo,
    long UserId,
    string Username,
    decimal TotalAmount,
    OrderStatus Status,
    string ShippingAddress,
    string? Remark,
    DateTime ExpireAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    PaymentResponse? Payment);
