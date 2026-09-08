namespace RetailSystem.Api.Models;

public enum OrderStatus
{
    PendingPayment = 0,
    PendingShipment = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentMethod
{
    Alipay = 0,
    WeChat = 1,
    CreditCard = 2
}

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3
}

public sealed class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string OrderNo { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public required string ShippingAddress { get; set; }
    public string? Remark { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public Payment? Payment { get; set; }
    public ICollection<ProductReview> Reviews { get; set; } = [];
    public ICollection<CustomerServiceTicket> Tickets { get; set; } = [];
}

public sealed class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class Payment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}
