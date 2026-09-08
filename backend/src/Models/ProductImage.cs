namespace RetailSystem.Api.Models;

public sealed class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
