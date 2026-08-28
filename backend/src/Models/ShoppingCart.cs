namespace RetailSystem.Api.Models;

public sealed class ShoppingCart
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = [];
}

public sealed class CartItem
{
    public long Id { get; set; }
    public long CartId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }

    public ShoppingCart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
