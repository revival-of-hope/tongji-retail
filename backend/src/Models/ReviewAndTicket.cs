namespace RetailSystem.Api.Models;

public enum TicketStatus
{
    Pending = 0,
    Processing = 1,
    Resolved = 2,
    Closed = 3
}

public sealed class ProductReview
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public long OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
    public Order Order { get; set; } = null!;
}

public sealed class CustomerServiceTicket
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? OrderId { get; set; }
    public long? AssignedTo { get; set; }
    public required string Subject { get; set; }
    public required string Description { get; set; }
    public string? Reply { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public User? AssignedUser { get; set; }
}
