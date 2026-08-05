namespace RetailSystem.Api.Models;

public enum MerchantStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public sealed class Merchant
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string StoreName { get; set; }
    public string? Description { get; set; }
    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = [];
}
