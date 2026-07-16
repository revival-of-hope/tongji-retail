using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class OrderFlowTests
{
    [Fact]
    public async Task Customer_Can_Add_Order_Pay_And_Merchant_Can_Ship()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        var productsResponse = await client.GetAsync("/api/products/?pageSize=1");
        productsResponse.EnsureSuccessStatusCode();
        var products = await productsResponse.ReadDataAsync<PagedResponse<ProductListItem>>();
        var product = Assert.Single(products.Items);

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(product.Id, 2));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var cartItem = await addResponse.ReadDataAsync<CartItemResponse>();

        var orderResponse = await client.PostAsJsonAsync("/api/orders/", new CreateOrderRequest(
            [cartItem.CartItemId], "上海市杨浦区四平路 1239 号", "integration test"));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = await orderResponse.ReadDataAsync<OrderDetail>();
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(product.Price * 2, order.TotalAmount);

        var payResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        var paidOrder = await payResponse.ReadDataAsync<OrderDetail>();
        Assert.Equal(OrderStatus.PendingShipment, paidOrder.Status);
        Assert.Equal(PaymentStatus.Success, paidOrder.Payment?.Status);

        await client.LoginAsync("merchant", "Merchant123!");
        var shipResponse = await client.PutAsync($"/api/orders/{order.Id}/ship", null);
        Assert.Equal(HttpStatusCode.OK, shipResponse.StatusCode);
        var shippedOrder = await shipResponse.ReadDataAsync<OrderDetail>();
        Assert.Equal(OrderStatus.Shipped, shippedOrder.Status);
    }

    [Fact]
    public async Task Cancelling_Paid_Order_Marks_Payment_Refunded()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        var productPage = await (await client.GetAsync("/api/products/?pageSize=1"))
            .ReadDataAsync<PagedResponse<ProductListItem>>();
        var add = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(productPage.Items[0].Id, 1));
        var cartItem = await add.ReadDataAsync<CartItemResponse>();
        var created = await client.PostAsJsonAsync("/api/orders/", new CreateOrderRequest([cartItem.CartItemId], "测试地址", null));
        var order = await created.ReadDataAsync<OrderDetail>();
        await client.PostAsJsonAsync($"/api/orders/{order.Id}/pay", new PayOrderRequest(PaymentMethod.WeChat));

        var cancelled = await client.PutAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var result = await cancelled.ReadDataAsync<OrderDetail>();
        Assert.Equal(OrderStatus.Cancelled, result.Status);
        Assert.Equal(PaymentStatus.Refunded, result.Payment?.Status);
    }

    [Fact]
    public async Task Checkout_Rejects_Items_From_Different_Merchants()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        long firstProductId;
        long secondProductId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = db.Categories.First();
            firstProductId = db.Products
                .Where(x => x.Status == ProductStatus.OnSale)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .First();

            var secondMerchantUser = new User
            {
                Username = "merchant_two",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Merchant123!"),
                Role = UserRole.Merchant,
                ShoppingCart = new ShoppingCart()
            };
            var secondMerchant = new Merchant
            {
                User = secondMerchantUser,
                StoreName = "第二商店",
                Status = MerchantStatus.Approved
            };
            var secondProduct = new Product
            {
                Merchant = secondMerchant,
                Category = category,
                Name = "第二商家商品",
                Price = 10m,
                StockQuantity = 10,
                Status = ProductStatus.OnSale
            };
            db.Products.Add(secondProduct);
            await db.SaveChangesAsync();
            secondProductId = secondProduct.Id;
        }

        var firstAdd = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(firstProductId, 1));
        var firstItem = await firstAdd.ReadDataAsync<CartItemResponse>();
        var secondAdd = await client.PostAsJsonAsync("/api/cart/items", new AddCartItemRequest(secondProductId, 1));
        var secondItem = await secondAdd.ReadDataAsync<CartItemResponse>();

        var response = await client.PostAsJsonAsync("/api/orders/", new CreateOrderRequest(
            [firstItem.CartItemId, secondItem.CartItemId], "测试地址", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
