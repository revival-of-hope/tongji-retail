using RetailSystem.Api.Models;

namespace RetailSystem.Api.Contracts;

public sealed record CategoryResponse(
    long Id,
    string Name,
    int SortOrder,
    IReadOnlyList<CategoryResponse> Children);

public sealed record ProductQuery(
    int PageIndex = 1,
    int PageSize = 20,
    long? CategoryId = null,
    string? Keyword = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "newest");

public sealed record ProductListItem(
    long Id,
    string Name,
    decimal Price,
    int StockQuantity,
    int SoldCount,
    decimal AvgRating,
    int ReviewCount,
    ProductStatus Status,
    string StoreName,
    string CategoryName,
    string? MainImageUrl,
    DateTime CreatedAt);

public sealed record ProductDetail(
    long Id,
    long MerchantId,
    string StoreName,
    long CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    int SoldCount,
    decimal AvgRating,
    int ReviewCount,
    ProductStatus Status,
    IReadOnlyList<string> ImageUrls,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateProductRequest(
    long CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    IReadOnlyList<string>? ImageUrls);

public sealed record UpdateProductRequest(
    long CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    IReadOnlyList<string>? ImageUrls);

public sealed record ReviewProductRequest(bool Approved);

public sealed record CreateReviewRequest(long OrderId, int Rating, string? Comment);

public sealed record ProductReviewResponse(
    long Id,
    long ProductId,
    long OrderId,
    string Username,
    int Rating,
    string? Comment,
    DateTime CreatedAt);
