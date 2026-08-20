using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class OrderBoundaryTests
{
    [Fact]
    public async Task PendingPayment_Order_Cannot_Be_Completed()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        var response = await client.PutAsync(
            $"/api/orders/{order.Id}/complete",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task PendingPayment_Order_Cannot_Be_Shipped()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.PutAsync(
            $"/api/orders/{order.Id}/ship",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Order_Cannot_Be_Paid_Twice()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        var firstPayment = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            firstPayment.StatusCode);

        var secondPayment = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.WeChat));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondPayment.StatusCode);
    }

    [Fact]
    public async Task Shipped_Order_Cannot_Be_Cancelled()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        var payResponse = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        await client.LoginAsync("merchant", "Merchant123!");

        var shipResponse = await client.PutAsync(
            $"/api/orders/{order.Id}/ship",
            null);

        Assert.Equal(
            HttpStatusCode.OK,
            shipResponse.StatusCode);

        await client.LoginAsync("customer", "Customer123!");

        var cancelResponse = await client.PutAsync(
            $"/api/orders/{order.Id}/cancel",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            cancelResponse.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_Ship_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        var response = await client.PutAsync(
            $"/api/orders/{order.Id}/ship",
            null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Pay_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Complete_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.PutAsync(
            $"/api/orders/{order.Id}/complete",
            null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Customer_Can_Get_Own_Order_Detail()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var order = await CreateOrderAsync(client);

        var response = await client.GetAsync(
            $"/api/orders/{order.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var detail =
            await response.ReadDataAsync<OrderDetail>();

        Assert.Equal(order.Id, detail.Id);
        Assert.Equal("customer", detail.Username);
        Assert.Equal(
            OrderStatus.PendingPayment,
            detail.Status);
    }

    private static async Task<OrderDetail> CreateOrderAsync(
        HttpClient client)
    {
        await client.LoginAsync(
            "customer",
            "Customer123!");

        var productsResponse =
            await client.GetAsync("/api/products/?pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            productsResponse.StatusCode);

        var products =
            await productsResponse
                .ReadDataAsync<PagedResponse<ProductListItem>>();

        var product =
            Assert.Single(products.Items);

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
                    "上海市杨浦区四平路1239号",
                    "订单边界测试"));

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);

        return await orderResponse
            .ReadDataAsync<OrderDetail>();
    }
}