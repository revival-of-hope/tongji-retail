using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    private static async Task<IResult> GetProductsAsync(
        [AsParameters] ProductQuery query,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationError = Validation.ProductQuery(
            query.PageIndex,
            query.PageSize,
            query.Keyword,
            query.MinPrice,
            query.MaxPrice,
            query.SortBy);
        if (validationError is not null) return ApiResults.BadRequest(validationError);
        if (query.CategoryId is <= 0) return ApiResults.BadRequest("商品分类编号无效");

        IQueryable<Product> source = db.Products.AsNoTracking()
            .Include(product => product.Merchant)
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Where(product => product.Status == ProductStatus.OnSale && product.StockQuantity > 0);

        if (query.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryAndDescendantIdsAsync(query.CategoryId.Value, db, cancellationToken);
            if (categoryIds is null) return ApiResults.BadRequest("商品分类不存在");
            source = source.Where(product => categoryIds.Contains(product.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(product =>
                product.Name.Contains(keyword) ||
                (product.Description != null && product.Description.Contains(keyword)));
        }

        if (query.MinPrice.HasValue) source = source.Where(product => product.Price >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue) source = source.Where(product => product.Price <= query.MaxPrice.Value);

        source = query.SortBy.ToLowerInvariant() switch
        {
            "sales" => source.OrderByDescending(product => product.SoldCount).ThenByDescending(product => product.Id),
            "rating" => source.OrderByDescending(product => product.AvgRating).ThenByDescending(product => product.ReviewCount),
            "price_asc" => source.OrderBy(product => product.Price).ThenByDescending(product => product.Id),
            "price_desc" => source.OrderByDescending(product => product.Price).ThenByDescending(product => product.Id),
            _ => source.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var entities = await source
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResponse<ProductListItem>(
            entities.Select(product => product.ToListItem()).ToArray(),
            query.PageIndex,
            query.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
        return ApiResults.Ok(result);
    }

    private static async Task<IResult> GetProductAsync(
        long id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await ProductQueryBase(db)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Status == ProductStatus.OnSale) return ApiResults.Ok(product.ToDetail());

        var canInspectNonPublicProduct = principal.Identity?.IsAuthenticated == true &&
            (principal.IsInRole(nameof(UserRole.Admin)) ||
             (principal.IsInRole(nameof(UserRole.Merchant)) && product.Merchant.UserId == principal.GetUserId()));

        return canInspectNonPublicProduct
            ? ApiResults.Ok(product.ToDetail())
            : ApiResults.NotFound("商品不存在或未上架");
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = Validation.Product(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.ImageUrls);
        if (error is not null) return ApiResults.BadRequest(error);
        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
            return ApiResults.BadRequest("商品分类不存在");

        var merchant = await db.Merchants.SingleOrDefaultAsync(
            item => item.UserId == principal.GetUserId(),
            cancellationToken);
        if (merchant is null || merchant.Status != MerchantStatus.Approved)
            return ApiResults.Forbidden("商家尚未通过审核");

        var product = new Product
        {
            MerchantId = merchant.Id,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Status = ProductStatus.PendingReview,
            Images = BuildImages(request.ImageUrls)
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        product = await ProductQueryBase(db).SingleAsync(item => item.Id == product.Id, cancellationToken);
        return ApiResults.Created($"/api/products/{product.Id}", product.ToDetail(), "商品已提交审核");
    }

    private static async Task<IResult> UpdateProductAsync(
        long id,
        UpdateProductRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = Validation.Product(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.ImageUrls);
        if (error is not null) return ApiResults.BadRequest(error);
        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
            return ApiResults.BadRequest("商品分类不存在");

        var product = await db.Products
            .Include(item => item.Images)
            .Include(item => item.Merchant)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Merchant.UserId != principal.GetUserId())
            return ApiResults.Forbidden("不能修改其他商家的商品");

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;
        product.Status = ProductStatus.PendingReview;
        db.ProductImages.RemoveRange(product.Images);
        product.Images = BuildImages(request.ImageUrls);
        await db.SaveChangesAsync(cancellationToken);

        product = await ProductQueryBase(db).SingleAsync(item => item.Id == id, cancellationToken);
        return ApiResults.Ok(product.ToDetail(), "商品已更新并重新提交审核");
    }

    private static async Task<IResult> ReviewProductAsync(
        long id,
        ReviewProductRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await ProductQueryBase(db).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return ApiResults.NotFound("商品不存在");
        if (product.Status != ProductStatus.PendingReview) return ApiResults.Conflict("该商品已完成审核");

        product.Status = request.Approved ? ProductStatus.OnSale : ProductStatus.Rejected;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(product.ToDetail(), request.Approved ? "商品审核通过" : "商品审核拒绝");
    }

    private static async Task<IResult> GetReviewsAsync(long id, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(product => product.Id == id, cancellationToken))
            return ApiResults.NotFound("商品不存在");

        var reviews = await db.ProductReviews
            .AsNoTracking()
            .Include(review => review.User)
            .Where(review => review.ProductId == id)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<ProductReviewResponse>>(
            reviews.Select(review => review.ToResponse()).ToArray());
    }

    private static async Task<IResult> CreateReviewAsync(
        long id,
        CreateReviewRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return ApiResults.BadRequest("评分必须为 1—5 星");
        if (request.Comment?.Trim().Length > 1000) return ApiResults.BadRequest("评价内容不能超过 1000 个字符");

        IDbContextTransaction? transaction = null;
        try
        {
            if (db.Database.IsRelational())
                transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var userId = principal.GetUserId();
            var order = await db.Orders
                .Include(item => item.Items)
                .SingleOrDefaultAsync(
                    item => item.Id == request.OrderId && item.UserId == userId,
                    cancellationToken);
            if (order is null || order.Status != OrderStatus.Completed || order.Items.All(item => item.ProductId != id))
                return ApiResults.BadRequest("只能评价本人已完成订单中的商品");

            if (await db.ProductReviews.AnyAsync(
                    review => review.OrderId == request.OrderId && review.ProductId == id,
                    cancellationToken))
                return ApiResults.Conflict("该订单商品已经评价");

            var review = new ProductReview
            {
                ProductId = id,
                UserId = userId,
                OrderId = request.OrderId,
                Rating = request.Rating,
                Comment = NormalizeOptional(request.Comment)
            };
            db.ProductReviews.Add(review);
            await db.SaveChangesAsync(cancellationToken);

            var aggregate = await db.ProductReviews
                .Where(item => item.ProductId == id)
                .GroupBy(_ => 1)
                .Select(group => new { Count = group.Count(), Average = group.Average(item => item.Rating) })
                .SingleAsync(cancellationToken);
            var product = await db.Products.SingleAsync(item => item.Id == id, cancellationToken);
            product.ReviewCount = aggregate.Count;
            product.AvgRating = Math.Round((decimal)aggregate.Average, 2);
            await db.SaveChangesAsync(cancellationToken);

            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            review.User = await db.Users.AsNoTracking().SingleAsync(item => item.Id == userId, cancellationToken);
            return ApiResults.Created($"/api/products/{id}/reviews", review.ToResponse(), "评价成功");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static async Task<IResult> GetCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .ToListAsync(cancellationToken);

        CategoryResponse Build(Category category) => new(
            category.Id,
            category.Name,
            category.SortOrder,
            categories.Where(item => item.ParentId == category.Id).Select(Build).ToArray());

        var roots = categories.Where(category => category.ParentId is null).Select(Build).ToArray();
        return ApiResults.Ok<IReadOnlyList<CategoryResponse>>(roots);
    }

    private static async Task<HashSet<long>?> GetCategoryAndDescendantIdsAsync(
        long categoryId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Select(category => new { category.Id, category.ParentId })
            .ToListAsync(cancellationToken);
        if (categories.All(category => category.Id != categoryId)) return null;

        var result = new HashSet<long> { categoryId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var category in categories)
            {
                if (category.ParentId.HasValue && result.Contains(category.ParentId.Value) && result.Add(category.Id))
                    changed = true;
            }
        }

        return result;
    }

    private static IQueryable<Product> ProductQueryBase(AppDbContext db) => db.Products
        .Include(product => product.Merchant)
        .Include(product => product.Category)
        .Include(product => product.Images);

    private static List<ProductImage> BuildImages(IReadOnlyList<string>? urls)
    {
        var clean = (urls ?? [])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Validation.MaxImageCount)
            .ToArray();
        return clean.Select((url, index) => new ProductImage
        {
            ImageUrl = url,
            IsMain = index == 0,
            SortOrder = index
        }).ToList();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
