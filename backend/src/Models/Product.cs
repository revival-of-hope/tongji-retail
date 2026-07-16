namespace RetailSystem.Api.Models;

public enum ProductStatus
{
    PendingReview = 0,
    OnSale = 1,
    OffShelf = 2,
    Rejected = 3
}

public sealed class Product
{
    public long Id { get; set; }
    public long MerchantId { get; set; }
    public long CategoryId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int SoldCount { get; set; }
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.PendingReview;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Merchant Merchant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];
}
