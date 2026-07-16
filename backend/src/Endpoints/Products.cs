using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products");

        products.MapGet("/", GetProductsAsync)
            .WithName("GetProducts")
            .WithSummary("分页搜索与筛选已上架商品")
            .Produces<ApiEnvelope<PagedResponse<ProductListItem>>>()
            .WithStandardErrors();

        products.MapGet("/{id:long}", GetProductAsync)
            .WithName("GetProduct")
            .WithSummary("获取商品详情")
            .Produces<ApiEnvelope<ProductDetail>>()
            .WithStandardErrors();

        products.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithSummary("商家发布待审核商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<ProductDetail>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        products.MapPut("/{id:long}", UpdateProductAsync)
            .WithName("UpdateProduct")
            .WithSummary("商家修改自己的商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Merchant)))
            .Produces<ApiEnvelope<ProductDetail>>()
            .WithStandardErrors();

        products.MapPut("/{id:long}/review", ReviewProductAsync)
            .WithName("ReviewProduct")
            .WithSummary("管理员审核商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<ProductDetail>>()
            .WithStandardErrors();

        products.MapGet("/{id:long}/reviews", GetReviewsAsync)
            .WithName("GetProductReviews")
            .WithSummary("获取商品评价")
            .Produces<ApiEnvelope<IReadOnlyList<ProductReviewResponse>>>()
            .WithStandardErrors();

        products.MapPost("/{id:long}/reviews", CreateReviewAsync)
            .WithName("CreateProductReview")
            .WithSummary("顾客评价已完成订单中的商品")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<ProductReviewResponse>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        app.MapGet("/api/categories", GetCategoriesAsync)
            .WithTags("Categories")
            .WithName("GetCategories")
            .WithSummary("获取树形商品分类")
            .Produces<ApiEnvelope<IReadOnlyList<CategoryResponse>>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> GetProductsAsync([AsParameters] ProductQuery query, AppDbContext db, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var source = db.Products.AsNoTracking()
            .Include(x => x.Merchant)
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.Status == ProductStatus.OnSale && x.StockQuantity > 0);

        if (query.CategoryId.HasValue) source = source.Where(x => x.CategoryId == query.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)));
        }
        if (query.MinPrice.HasValue) source = source.Where(x => x.Price >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue) source = source.Where(x => x.Price <= query.MaxPrice.Value);

        source = query.SortBy.ToLowerInvariant() switch
        {
            "sales" => source.OrderByDescending(x => x.SoldCount).ThenByDescending(x => x.Id),
            "rating" => source.OrderByDescending(x => x.AvgRating).ThenByDescending(x => x.ReviewCount),
            "price_asc" => source.OrderBy(x => x.Price).ThenByDescending(x => x.Id),
            "price_desc" => source.OrderByDescending(x => x.Price).ThenByDescending(x => x.Id),
            _ => source.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var entities = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var result = new PagedResponse<ProductListItem>(
            entities.Select(x => x.ToListItem()).ToArray(),
            pageIndex,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
        return ApiResults.Ok(result);
    }

    private static async Task<IResult> GetProductAsync(long id, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var product = await ProductQueryBase(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Status == ProductStatus.OnSale) return ApiResults.Ok(product.ToDetail());

        var canInspectNonPublicProduct = principal.Identity?.IsAuthenticated == true &&
            (principal.IsInRole(nameof(UserRole.Admin)) ||
             (principal.IsInRole(nameof(UserRole.Merchant)) && product.Merchant.UserId == principal.GetUserId()));

        return canInspectNonPublicProduct
            ? ApiResults.Ok(product.ToDetail())
            : ApiResults.NotFound("商品不存在或未上架");
    }

    private static async Task<IResult> CreateProductAsync(CreateProductRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var error = Validation.Product(request.Name, request.Price, request.StockQuantity);
        if (error is not null) return ApiResults.BadRequest(error);
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken)) return ApiResults.BadRequest("商品分类不存在");

        var merchant = await db.Merchants.SingleOrDefaultAsync(x => x.UserId == principal.GetUserId(), cancellationToken);
        if (merchant is null || merchant.Status != MerchantStatus.Approved) return ApiResults.Forbidden("商家尚未通过审核");

        var product = new Product
        {
            MerchantId = merchant.Id,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Status = ProductStatus.PendingReview,
            Images = BuildImages(request.ImageUrls)
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        product = await ProductQueryBase(db).SingleAsync(x => x.Id == product.Id, cancellationToken);
        return ApiResults.Created($"/api/products/{product.Id}", product.ToDetail(), "商品已提交审核");
    }

    private static async Task<IResult> UpdateProductAsync(long id, UpdateProductRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var error = Validation.Product(request.Name, request.Price, request.StockQuantity);
        if (error is not null) return ApiResults.BadRequest(error);
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken)) return ApiResults.BadRequest("商品分类不存在");

        var product = await db.Products.Include(x => x.Images).Include(x => x.Merchant)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Merchant.UserId != principal.GetUserId()) return ApiResults.Forbidden("不能修改其他商家的商品");

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;
        product.Status = ProductStatus.PendingReview;
        db.ProductImages.RemoveRange(product.Images);
        product.Images = BuildImages(request.ImageUrls);
        await db.SaveChangesAsync(cancellationToken);

        product = await ProductQueryBase(db).SingleAsync(x => x.Id == id, cancellationToken);
        return ApiResults.Ok(product.ToDetail(), "商品已更新并重新提交审核");
    }

    private static async Task<IResult> ReviewProductAsync(long id, ReviewProductRequest request, AppDbContext db, CancellationToken cancellationToken)
    {
        var product = await ProductQueryBase(db).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Status != ProductStatus.PendingReview) return ApiResults.Conflict("该商品已完成审核");
        product.Status = request.Approved ? ProductStatus.OnSale : ProductStatus.Rejected;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(product.ToDetail(), request.Approved ? "商品审核通过" : "商品审核拒绝");
    }

    private static async Task<IResult> GetReviewsAsync(long id, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(x => x.Id == id, cancellationToken)) return ApiResults.NotFound("商品不存在");
        var reviews = await db.ProductReviews.AsNoTracking().Include(x => x.User)
            .Where(x => x.ProductId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<ProductReviewResponse>>(reviews.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> CreateReviewAsync(long id, CreateReviewRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return ApiResults.BadRequest("评分必须为 1—5 星");
        if (request.Comment?.Length > 1000) return ApiResults.BadRequest("评价内容不能超过 1000 个字符");
        var userId = principal.GetUserId();
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == request.OrderId && x.UserId == userId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Completed || order.Items.All(x => x.ProductId != id))
            return ApiResults.BadRequest("只能评价本人已完成订单中的商品");
        if (await db.ProductReviews.AnyAsync(x => x.OrderId == request.OrderId && x.ProductId == id, cancellationToken))
            return ApiResults.Conflict("该订单商品已经评价");

        var review = new ProductReview
        {
            ProductId = id,
            UserId = userId,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };
        db.ProductReviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        var aggregate = await db.ProductReviews.Where(x => x.ProductId == id)
            .GroupBy(_ => 1).Select(g => new { Count = g.Count(), Average = g.Average(x => x.Rating) })
            .SingleAsync(cancellationToken);
        var product = await db.Products.SingleAsync(x => x.Id == id, cancellationToken);
        product.ReviewCount = aggregate.Count;
        product.AvgRating = Math.Round((decimal)aggregate.Average, 2);
        await db.SaveChangesAsync(cancellationToken);
        review.User = await db.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        return ApiResults.Created($"/api/products/{id}/reviews", review.ToResponse(), "评价成功");
    }

    private static async Task<IResult> GetCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        CategoryResponse Build(Category category) => new(
            category.Id,
            category.Name,
            category.SortOrder,
            categories.Where(x => x.ParentId == category.Id).Select(Build).ToArray());
        var roots = categories.Where(x => x.ParentId is null).Select(Build).ToArray();
        return ApiResults.Ok<IReadOnlyList<CategoryResponse>>(roots);
    }

    private static IQueryable<Product> ProductQueryBase(AppDbContext db) => db.Products
        .Include(x => x.Merchant)
        .Include(x => x.Category)
        .Include(x => x.Images);

    private static List<ProductImage> BuildImages(IReadOnlyList<string>? urls)
    {
        var clean = (urls ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().Take(8).ToArray();
        return clean.Select((url, index) => new ProductImage { ImageUrl = url, IsMain = index == 0, SortOrder = index }).ToList();
    }
}
