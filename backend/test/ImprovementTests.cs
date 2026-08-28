using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Tests;

public sealed class ImprovementTests
{
    [Fact]
    public void Model_Still_Uses_Exactly_Twelve_Core_Tables_With_Check_Constraints()
    {
        using var factory = new RetailApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var designTimeModel = db.GetService<IDesignTimeModel>().Model;
        var tables = designTimeModel.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(table => table is not null)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(12, tables.Length);
        var products = designTimeModel.FindEntityType(typeof(Product));
        Assert.NotNull(products);
        Assert.NotEmpty(products!.GetCheckConstraints());
    }

    [Fact]
    public async Task Serializable_Executor_Retries_Oracle_08177_With_A_Fresh_Context()
    {
        await using var factory = new RetailApiFactory();
        using var scope = factory.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<SerializableTransactionExecutor>();
        var attempts = 0;
        var contextIds = new List<Guid>();

        var result = await executor.ExecuteAsync(async (db, cancellationToken) =>
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            attempts++;
            contextIds.Add(db.ContextId.InstanceId);

            if (attempts == 1)
            {
                throw new DbUpdateException(
                    "Simulated Oracle write failure",
                    new InvalidOperationException("ORA-08177: can't serialize access for this transaction"));
            }

            return 42;
        }, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(2, attempts);
        Assert.Equal(2, contextIds.Distinct().Count());
    }

    [Fact]
    public async Task Serializable_Executor_Does_Not_Retry_Non_Transient_Constraint_Errors()
    {
        await using var factory = new RetailApiFactory();
        using var scope = factory.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<SerializableTransactionExecutor>();
        var attempts = 0;

        await Assert.ThrowsAsync<DbUpdateException>(() => executor.ExecuteAsync<int>((_, _) =>
        {
            attempts++;
            throw new DbUpdateException(
                "Simulated unique constraint failure",
                new InvalidOperationException("ORA-00001: unique constraint violated"));
        }, CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Parent_Category_Filter_Includes_Descendant_Products()
    {
        await using var factory = new RetailApiFactory();
        long parentId;
        long childProductId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var parent = await db.Categories.OrderBy(category => category.Id).FirstAsync();
            var merchant = await db.Merchants.FirstAsync(item => item.Status == MerchantStatus.Approved);
            var child = new Category { Name = "子分类", Parent = parent, SortOrder = 1 };
            var product = new Product
            {
                Merchant = merchant,
                Category = child,
                Name = "子分类商品",
                Price = 25m,
                StockQuantity = 10,
                Status = ProductStatus.OnSale
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            parentId = parent.Id;
            childProductId = product.Id;
        }

        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/products/?categoryId={parentId}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadDataAsync<PagedResponse<ProductListItem>>();
        Assert.Contains(page.Items, product => product.Id == childProductId);
    }

    [Fact]
    public async Task Invalid_Product_Filter_Returns_Envelope_Bad_Request()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/?minPrice=100&maxPrice=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();
        Assert.NotNull(envelope);
        Assert.Equal(400, envelope.Code);
    }

    [Fact]
    public async Task Reading_Orders_Automatically_Cancels_Expired_Pending_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        var order = await CreatePendingOrderAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Orders.SingleAsync(item => item.Id == order.Id);
            entity.ExpireAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/orders/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.ReadDataAsync<IReadOnlyList<OrderSummary>>();
        Assert.Equal(OrderStatus.Cancelled, Assert.Single(orders, item => item.Id == order.Id).Status);
    }

    [Fact]
    public async Task Oracle_Unspecified_DateTimes_Are_Serialized_As_Utc()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        var order = await CreatePendingOrderAsync(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Orders.SingleAsync(item => item.Id == order.Id);

            // Oracle DATE/TIMESTAMP keeps the clock value but does not retain
            // DateTime.Kind. Simulate that materialization behavior here.
            entity.ExpireAt = DateTime.SpecifyKind(entity.ExpireAt, DateTimeKind.Unspecified);
            entity.CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Unspecified);
            entity.UpdatedAt = DateTime.SpecifyKind(entity.UpdatedAt, DateTimeKind.Unspecified);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");

        foreach (var propertyName in new[] { "expireAt", "createdAt", "updatedAt" })
        {
            var text = data.GetProperty(propertyName).GetString();
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.True(
                text!.EndsWith("Z", StringComparison.OrdinalIgnoreCase),
                $"{propertyName} 应以 UTC 的 Z 后缀输出，实际值为：{text}");

            var parsed = DateTimeOffset.Parse(
                text!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            Assert.Equal(TimeSpan.Zero, parsed.Offset);
        }
    }

    [Fact]
    public async Task Completed_Order_Item_Is_Marked_Reviewed_After_Review()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");
        var order = await CreatePendingOrderAsync(client);

        var pay = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);

        await client.LoginAsync("merchant", "Merchant123!");
        var ship = await client.PutAsync($"/api/orders/{order.Id}/ship", null);
        Assert.Equal(HttpStatusCode.OK, ship.StatusCode);

        await client.LoginAsync("customer", "Customer123!");
        var complete = await client.PutAsync($"/api/orders/{order.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var review = await client.PostAsApiJsonAsync(
            $"/api/products/{order.Items[0].ProductId}/reviews",
            new CreateReviewRequest(order.Id, 5, "很好"));
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);

        var ordersResponse = await client.GetAsync("/api/orders/");
        var orders = await ordersResponse.ReadDataAsync<IReadOnlyList<OrderSummary>>();
        var reviewedOrder = Assert.Single(orders, item => item.Id == order.Id);
        Assert.True(Assert.Single(reviewedOrder.Items).Reviewed);
    }

    [Fact]
    public async Task Daily_Sales_Uses_Payment_Time_Instead_Of_Order_Creation_Time()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");
        var order = await CreatePendingOrderAsync(client);

        var pay = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);

        var paymentTime = DateTime.UtcNow.Date.AddHours(12);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Orders.Include(item => item.Payment).SingleAsync(item => item.Id == order.Id);
            entity.CreatedAt = paymentTime.AddDays(-60);
            entity.Payment!.PaidAt = paymentTime;
            await db.SaveChangesAsync();
        }

        await client.LoginAsync("admin", "Admin123!");
        var response = await client.GetAsync("/api/reports/daily-sales");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var points = await response.ReadDataAsync<IReadOnlyList<DailySalesPoint>>();
        var today = Assert.Single(points, point => point.Date == DateOnly.FromDateTime(paymentTime));
        Assert.True(today.Sales >= order.TotalAmount);
    }

    [Fact]
    public async Task Numeric_Enum_Value_Is_Rejected_As_Bad_Request()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");
        var order = await CreatePendingOrderAsync(client);

        // Deliberately bypass PostAsApiJsonAsync so the payload contains a JSON number.
        // The production API must reject numeric enum input instead of accepting 999.
        var response = await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new { paymentMethod = 999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();
        Assert.NotNull(envelope);
        Assert.Equal(400, envelope.Code);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
    }

    private static async Task<OrderDetail> CreatePendingOrderAsync(HttpClient client)
    {
        var productsResponse = await client.GetAsync("/api/products/?pageSize=1");
        var products = await productsResponse.ReadDataAsync<PagedResponse<ProductListItem>>();
        var product = Assert.Single(products.Items);

        var addResponse = await client.PostAsApiJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(product.Id, 1));
        var cartItem = await addResponse.ReadDataAsync<CartItemResponse>();

        var orderResponse = await client.PostAsApiJsonAsync(
            "/api/orders/",
            new CreateOrderRequest([cartItem.CartItemId], "测试地址", null));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        return await orderResponse.ReadDataAsync<OrderDetail>();
    }
}
