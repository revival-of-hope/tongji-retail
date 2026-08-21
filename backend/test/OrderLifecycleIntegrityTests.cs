using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class OrderLifecycleIntegrityTests
{
    [Fact]
    public async Task Payment_Deducts_Stock_And_Increments_Sold_Count_Exactly_Once()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        var before =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        var paid =
            await payResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingShipment,
            paid.Status);

        Assert.NotNull(
            paid.Payment);

        Assert.Equal(
            PaymentStatus.Success,
            paid.Payment.Status);

        Assert.Equal(
            order.TotalAmount,
            paid.Payment.Amount);

        var afterPayment =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            before.StockQuantity - 2,
            afterPayment.StockQuantity);

        Assert.Equal(
            before.SoldCount + 2,
            afterPayment.SoldCount);

        var secondPay =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.WeChat));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondPay.StatusCode);

        var afterSecondPayment =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            afterPayment,
            afterSecondPayment);

        Assert.Equal(
            1,
            await GetPaymentCountAsync(
                factory,
                order.Id));
    }

    [Fact]
    public async Task Cancelling_Paid_Order_Restores_Inventory_And_Refunds_Payment()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        var original =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        var afterPayment =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            original.StockQuantity - 2,
            afterPayment.StockQuantity);

        Assert.Equal(
            original.SoldCount + 2,
            afterPayment.SoldCount);

        var cancelResponse =
            await client.PutAsync(
                $"/api/orders/{order.Id}/cancel",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            cancelResponse.StatusCode);

        var cancelled =
            await cancelResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Cancelled,
            cancelled.Status);

        Assert.NotNull(
            cancelled.Payment);

        Assert.Equal(
            PaymentStatus.Refunded,
            cancelled.Payment.Status);

        var afterCancellation =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            original.StockQuantity,
            afterCancellation.StockQuantity);

        Assert.Equal(
            original.SoldCount,
            afterCancellation.SoldCount);

        var secondCancel =
            await client.PutAsync(
                $"/api/orders/{order.Id}/cancel",
                null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondCancel.StatusCode);

        var afterSecondCancellation =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            afterCancellation,
            afterSecondCancellation);

        Assert.Equal(
            1,
            await GetPaymentCountAsync(
                factory,
                order.Id));
    }

    [Fact]
    public async Task Cancelling_Unpaid_Order_Does_Not_Change_Inventory()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        var before =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        var cancelResponse =
            await client.PutAsync(
                $"/api/orders/{order.Id}/cancel",
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            cancelResponse.StatusCode);

        var cancelled =
            await cancelResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Cancelled,
            cancelled.Status);

        Assert.Null(
            cancelled.Payment);

        var after =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            before,
            after);

        Assert.Equal(
            0,
            await GetPaymentCountAsync(
                factory,
                order.Id));
    }

    [Fact]
    public async Task Expired_Unpaid_Order_Is_Cancelled_Without_Changing_Inventory()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 1);

        var before =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        await ExpireOrderAsync(
            factory,
            order.Id);

        var response =
            await client.GetAsync(
                $"/api/orders/{order.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var expired =
            await response
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Cancelled,
            expired.Status);

        Assert.Null(
            expired.Payment);

        var after =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            before,
            after);

        Assert.Equal(
            0,
            await GetPaymentCountAsync(
                factory,
                order.Id));
    }

    [Fact]
    public async Task Paying_Expired_Order_Fails_Without_Changing_Inventory()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        var before =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        await ExpireOrderAsync(
            factory,
            order.Id);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.CreditCard));

        Assert.Equal(
            HttpStatusCode.Conflict,
            payResponse.StatusCode);

        var after =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            before,
            after);

        Assert.Equal(
            0,
            await GetPaymentCountAsync(
                factory,
                order.Id));

        var orderResponse =
            await client.GetAsync(
                $"/api/orders/{order.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            orderResponse.StatusCode);

        var current =
            await orderResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.Cancelled,
            current.Status);
    }

    [Fact]
    public async Task Insufficient_Stock_During_Payment_Does_Not_Partially_Update_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        await SetProductStockAsync(
            factory,
            product.Id,
            1);

        var before =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        var response =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var after =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            before,
            after);

        Assert.Equal(
            0,
            await GetPaymentCountAsync(
                factory,
                order.Id));

        var orderResponse =
            await client.GetAsync(
                $"/api/orders/{order.Id}");

        var current =
            await orderResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingPayment,
            current.Status);

        Assert.Null(
            current.Payment);
    }

    [Fact]
    public async Task Order_Price_Snapshot_Is_Not_Changed_By_Later_Product_Price_Update()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, order) =
            await CreatePendingOrderAsync(client, 2);

        var orderItem =
            Assert.Single(order.Items);

        var originalUnitPrice =
            orderItem.UnitPrice;

        var originalSubTotal =
            orderItem.SubTotal;

        var originalTotal =
            order.TotalAmount;

        await SetProductPriceAsync(
            factory,
            product.Id,
            product.Price + 123.45m);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        var paid =
            await payResponse
                .ReadDataAsync<OrderDetail>();

        var paidItem =
            Assert.Single(paid.Items);

        Assert.Equal(
            originalUnitPrice,
            paidItem.UnitPrice);

        Assert.Equal(
            originalSubTotal,
            paidItem.SubTotal);

        Assert.Equal(
            originalTotal,
            paid.TotalAmount);

        Assert.NotNull(
            paid.Payment);

        Assert.Equal(
            originalTotal,
            paid.Payment.Amount);

        var productState =
            await GetProductSnapshotAsync(
                factory,
                product.Id);

        Assert.Equal(
            product.Price + 123.45m,
            productState.Price);
    }

    private static async Task<(
        ProductListItem Product,
        OrderDetail Order)>
        CreatePendingOrderAsync(
            HttpClient client,
            int quantity)
    {
        await client.LoginAsync(
            "customer",
            "Customer123!");

        var productsResponse =
            await client.GetAsync(
                "/api/products/?pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            productsResponse.StatusCode);

        var products =
            await productsResponse
                .ReadDataAsync<
                    PagedResponse<ProductListItem>>();

        var product =
            Assert.Single(products.Items);

        Assert.True(
            product.StockQuantity >= quantity,
            "测试商品库存不足，无法创建测试订单");

        var addResponse =
            await client.PostAsApiJsonAsync(
                "/api/cart/items",
                new AddCartItemRequest(
                    product.Id,
                    quantity));

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
                    "订单生命周期完整性测试"));

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);

        var order =
            await orderResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingPayment,
            order.Status);

        Assert.Null(
            order.Payment);

        Assert.Equal(
            product.Price * quantity,
            order.TotalAmount);

        return (
            product,
            order);
    }

    private static async Task<ProductSnapshot>
        GetProductSnapshotAsync(
            RetailApiFactory factory,
            long productId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var product =
            await db.Products
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == productId);

        return new ProductSnapshot(
            product.StockQuantity,
            product.SoldCount,
            product.Price);
    }

    private static async Task<int>
        GetPaymentCountAsync(
            RetailApiFactory factory,
            long orderId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        return await db.Payments
            .AsNoTracking()
            .CountAsync(
                payment =>
                    payment.OrderId == orderId);
    }

    private static async Task ExpireOrderAsync(
        RetailApiFactory factory,
        long orderId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var order =
            await db.Orders.SingleAsync(
                item => item.Id == orderId);

        order.ExpireAt =
            DateTime.UtcNow.AddMinutes(-1);

        await db.SaveChangesAsync();
    }

    private static async Task SetProductStockAsync(
        RetailApiFactory factory,
        long productId,
        int stockQuantity)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var product =
            await db.Products.SingleAsync(
                item => item.Id == productId);

        product.StockQuantity =
            stockQuantity;

        await db.SaveChangesAsync();
    }

    private static async Task SetProductPriceAsync(
        RetailApiFactory factory,
        long productId,
        decimal price)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var product =
            await db.Products.SingleAsync(
                item => item.Id == productId);

        product.Price =
            price;

        product.UpdatedAt =
            DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private sealed record ProductSnapshot(
        int StockQuantity,
        int SoldCount,
        decimal Price);
}