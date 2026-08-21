using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class ProductIntegrityTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Product_Detail_Rejects_Invalid_Id(
        long productId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/products/{productId}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Product_Update_Rejects_Invalid_Id(
        long productId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var existing =
            await GetFirstPublicProductDetailAsync(client);

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/products/{productId}",
            ToUpdateRequest(existing));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Product_Review_Rejects_Invalid_Id(
        long productId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "admin",
            "Admin123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/products/{productId}/review",
            new ReviewProductRequest(true));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Product_Reviews_Reject_Invalid_Id(
        long productId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/products/{productId}/reviews");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Create_Product_Rejects_Invalid_Category_Id(
        long categoryId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response = await client.PostAsApiJsonAsync(
            "/api/products/",
            new CreateProductRequest(
                categoryId,
                "分类参数测试商品",
                "用于验证非法分类 ID",
                99m,
                10,
                []));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品分类编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Update_Product_Rejects_Invalid_Category_Id(
        long categoryId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var existing =
            await GetFirstPublicProductDetailAsync(client);

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/products/{existing.Id}",
            new UpdateProductRequest(
                categoryId,
                existing.Name,
                existing.Description,
                existing.Price,
                existing.StockQuantity,
                existing.ImageUrls));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品分类编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Create_Review_Rejects_Invalid_Product_Id(
        long productId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/products/{productId}/reviews",
            new CreateReviewRequest(
                1,
                5,
                "测试评价"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "商品编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Create_Review_Rejects_Invalid_Order_Id(
        long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var product =
            await GetFirstPublicProductAsync(client);

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/products/{product.Id}/reviews",
            new CreateReviewRequest(
                orderId,
                5,
                "测试评价"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "订单编号无效",
            error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_Review_Rejects_Invalid_Rating(
        int rating)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var product =
            await GetFirstPublicProductAsync(client);

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/products/{product.Id}/reviews",
            new CreateReviewRequest(
                1,
                rating,
                "测试评价"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "评分必须为 1—5 星",
            error.Message);
    }

    [Fact]
    public async Task Create_Review_Rejects_Overlong_Comment()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var product =
            await GetFirstPublicProductAsync(client);

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/products/{product.Id}/reviews",
            new CreateReviewRequest(
                1,
                5,
                new string(
                    'A',
                    1001)));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await ReadErrorAsync(response);

        Assert.Equal(
            "评价内容不能超过 1000 个字符",
            error.Message);
    }

    [Fact]
    public async Task Merchant_Cannot_Update_Another_Merchants_Product()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var existing =
            await GetFirstPublicProductDetailAsync(client);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var user = new User
            {
                Username = "merchant_two",
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "Merchant123!"),
                Role = UserRole.Merchant
            };

            var merchant = new Merchant
            {
                User = user,
                StoreName = "第二测试商店",
                Description = "所有权测试商家",
                Status = MerchantStatus.Approved
            };

            db.Merchants.Add(merchant);

            await db.SaveChangesAsync();
        }

        await client.LoginAsync(
            "merchant_two",
            "Merchant123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/products/{existing.Id}",
            ToUpdateRequest(existing));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Rejected_Product_Is_Hidden_From_Public_But_Visible_To_Owner()
    {
        await using var factory = new RetailApiFactory();

        using var merchantClient =
            factory.CreateClient();

        using var adminClient =
            factory.CreateClient();

        using var anonymousClient =
            factory.CreateClient();

        await merchantClient.LoginAsync(
            "merchant",
            "Merchant123!");

        var myProductsResponse =
            await merchantClient.GetAsync(
                "/api/merchants/my-products");

        Assert.Equal(
            HttpStatusCode.OK,
            myProductsResponse.StatusCode);

        var products =
            await myProductsResponse
                .ReadDataAsync<
                    IReadOnlyList<ProductListItem>>();

        Assert.NotEmpty(products);

        var product = products[0];

        var detailResponse =
            await merchantClient.GetAsync(
                $"/api/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            detailResponse.StatusCode);

        var detail =
            await detailResponse
                .ReadDataAsync<ProductDetail>();

        var updateResponse =
            await merchantClient.PutAsApiJsonAsync(
                $"/api/products/{product.Id}",
                ToUpdateRequest(
                    detail,
                    detail.Name + "（重新审核）"));

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var pending =
            await updateResponse
                .ReadDataAsync<ProductDetail>();

        Assert.Equal(
            ProductStatus.PendingReview,
            pending.Status);

        await adminClient.LoginAsync(
            "admin",
            "Admin123!");

        var reviewResponse =
            await adminClient.PutAsApiJsonAsync(
                $"/api/products/{product.Id}/review",
                new ReviewProductRequest(false));

        Assert.Equal(
            HttpStatusCode.OK,
            reviewResponse.StatusCode);

        var rejected =
            await reviewResponse
                .ReadDataAsync<ProductDetail>();

        Assert.Equal(
            ProductStatus.Rejected,
            rejected.Status);

        var anonymousDetail =
            await anonymousClient.GetAsync(
                $"/api/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            anonymousDetail.StatusCode);

        var anonymousReviews =
            await anonymousClient.GetAsync(
                $"/api/products/{product.Id}/reviews");

        Assert.Equal(
            HttpStatusCode.NotFound,
            anonymousReviews.StatusCode);

        var ownerDetail =
            await merchantClient.GetAsync(
                $"/api/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            ownerDetail.StatusCode);

        var ownerReviews =
            await merchantClient.GetAsync(
                $"/api/products/{product.Id}/reviews");

        Assert.Equal(
            HttpStatusCode.OK,
            ownerReviews.StatusCode);
    }

    [Fact]
    public async Task Completed_Order_Review_Updates_Product_Aggregates_And_Prevents_Duplicates()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreateCompletedOrderAsync(client);

        var reviewResponse =
            await client.PostAsApiJsonAsync(
                $"/api/products/{product.Id}/reviews",
                new CreateReviewRequest(
                    order.Id,
                    5,
                    "商品质量很好，评价流程测试。"));

        Assert.Equal(
            HttpStatusCode.Created,
            reviewResponse.StatusCode);

        var review =
            await reviewResponse
                .ReadDataAsync<ProductReviewResponse>();

        Assert.Equal(
            product.Id,
            review.ProductId);

        Assert.Equal(
            order.Id,
            review.OrderId);

        Assert.Equal(
            5,
            review.Rating);

        var reviewsResponse =
            await client.GetAsync(
                $"/api/products/{product.Id}/reviews");

        Assert.Equal(
            HttpStatusCode.OK,
            reviewsResponse.StatusCode);

        var reviews =
            await reviewsResponse
                .ReadDataAsync<
                    IReadOnlyList<ProductReviewResponse>>();

        Assert.Contains(
            reviews,
            item => item.Id == review.Id);

        var detailResponse =
            await client.GetAsync(
                $"/api/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            detailResponse.StatusCode);

        var detail =
            await detailResponse
                .ReadDataAsync<ProductDetail>();

        Assert.Equal(
            1,
            detail.ReviewCount);

        Assert.Equal(
            5m,
            detail.AvgRating);

        var orderResponse =
            await client.GetAsync(
                $"/api/orders/{order.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            orderResponse.StatusCode);

        var updatedOrder =
            await orderResponse
                .ReadDataAsync<OrderDetail>();

        var reviewedItem =
            Assert.Single(
                updatedOrder.Items,
                item => item.ProductId == product.Id);

        Assert.True(
            reviewedItem.Reviewed);

        var duplicateResponse =
            await client.PostAsApiJsonAsync(
                $"/api/products/{product.Id}/reviews",
                new CreateReviewRequest(
                    order.Id,
                    4,
                    "重复评价"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Pending_Order_Cannot_Be_Reviewed()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client);

        var response =
            await client.PostAsApiJsonAsync(
                $"/api/products/{product.Id}/reviews",
                new CreateReviewRequest(
                    order.Id,
                    5,
                    "订单还没有完成"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await ReadErrorAsync(response);

        Assert.Equal(
            "只能评价本人已完成订单中的商品",
            error.Message);
    }

    private static async Task<(
        ProductListItem Product,
        OrderDetail Order)>
        CreatePendingOrderAsync(
            HttpClient client)
    {
        await client.LoginAsync(
            "customer",
            "Customer123!");

        var product =
            await GetFirstPublicProductAsync(client);

        var addResponse =
            await client.PostAsApiJsonAsync(
                "/api/cart/items",
                new AddCartItemRequest(
                    product.Id,
                    1));

        Assert.Equal(
            HttpStatusCode.Created,
            addResponse.StatusCode);

        var cartItem =
            await addResponse
                .ReadDataAsync<CartItemResponse>();

        var orderResponse =
            await client.PostAsApiJsonAsync(
                "/api/orders/",
                new CreateOrderRequest(
                    [cartItem.CartItemId],
                    "上海市杨浦区四平路 1239 号",
                    "商品评价完整性测试"));

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);

        var order =
            await orderResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingPayment,
            order.Status);

        return (
            product,
            order);
    }

    private static async Task<(
        ProductListItem Product,
        OrderDetail Order)>
        CreateCompletedOrderAsync(
            HttpClient client)
    {
        var (product, order) =
            await CreatePendingOrderAsync(client);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        var paidOrder =
            await payResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingShipment,
            paidOrder.Status);

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var shipResponse =
            await client.PutAsync(
                $"/api/orders/{order.Id}/ship",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            shipResponse.StatusCode);

        var shippedOrder =
            await shipResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Shipped,
            shippedOrder.Status);

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var completeResponse =
            await client.PutAsync(
                $"/api/orders/{order.Id}/complete",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            completeResponse.StatusCode);

        var completedOrder =
            await completeResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Completed,
            completedOrder.Status);

        return (
            product,
            completedOrder);
    }

    private static async Task<ProductListItem>
        GetFirstPublicProductAsync(
            HttpClient client)
    {
        var response =
            await client.GetAsync(
                "/api/products/?pageIndex=1&pageSize=1&sortBy=newest");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var page =
            await response
                .ReadDataAsync<
                    PagedResponse<ProductListItem>>();

        return Assert.Single(
            page.Items);
    }

    private static async Task<ProductDetail>
        GetFirstPublicProductDetailAsync(
            HttpClient client)
    {
        var product =
            await GetFirstPublicProductAsync(client);

        var response =
            await client.GetAsync(
                $"/api/products/{product.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        return await response
            .ReadDataAsync<ProductDetail>();
    }

    private static UpdateProductRequest
        ToUpdateRequest(
            ProductDetail detail,
            string? name = null)
    {
        return new UpdateProductRequest(
            detail.CategoryId,
            name ?? detail.Name,
            detail.Description,
            detail.Price,
            detail.StockQuantity,
            detail.ImageUrls);
    }

    private static async Task<ApiEnvelope<object>>
        ReadErrorAsync(
            HttpResponseMessage response)
    {
        var result =
            await response.Content
                .ReadFromJsonAsync<ApiEnvelope<object>>();

        return result ??
            throw new InvalidOperationException(
                "接口没有返回预期的错误响应结构");
    }
}