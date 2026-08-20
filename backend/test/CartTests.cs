using System.Net;
using RetailSystem.Api.Contracts;

namespace RetailSystem.Api.Tests;

public sealed class CartTests
{
    [Fact]
    public async Task Customer_Can_Add_Update_And_Delete_Cart_Item()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var productsResponse = await client.GetAsync("/api/products/?pageSize=1");
        productsResponse.EnsureSuccessStatusCode();

        var products =
            await productsResponse.ReadDataAsync<PagedResponse<ProductListItem>>();

        var product = Assert.Single(products.Items);

        var addResponse = await client.PostAsApiJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(product.Id, 1));

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var addedItem =
            await addResponse.ReadDataAsync<CartItemResponse>();

        Assert.Equal(product.Id, addedItem.ProductId);
        Assert.Equal(1, addedItem.Quantity);

        var cartResponse = await client.GetAsync("/api/cart/");

        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);

        var cart =
            await cartResponse.ReadDataAsync<IReadOnlyList<CartItemResponse>>();

        Assert.Contains(
            cart,
            item => item.CartItemId == addedItem.CartItemId);

        var updateResponse = await client.PutAsApiJsonAsync(
            $"/api/cart/items/{addedItem.CartItemId}",
            new UpdateCartItemRequest(2));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedItem =
            await updateResponse.ReadDataAsync<CartItemResponse>();

        Assert.Equal(2, updatedItem.Quantity);

        var deleteResponse =
            await client.DeleteAsync($"/api/cart/items/{addedItem.CartItemId}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var finalCartResponse = await client.GetAsync("/api/cart/");

        Assert.Equal(HttpStatusCode.OK, finalCartResponse.StatusCode);

        var finalCart =
            await finalCartResponse.ReadDataAsync<IReadOnlyList<CartItemResponse>>();

        Assert.DoesNotContain(
            finalCart,
            item => item.CartItemId == addedItem.CartItemId);
    }

    [Fact]
    public async Task Add_Item_With_Zero_Quantity_Returns_BadRequest()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var productsResponse = await client.GetAsync("/api/products/?pageSize=1");
        var products =
            await productsResponse.ReadDataAsync<PagedResponse<ProductListItem>>();

        var product = Assert.Single(products.Items);

        var response = await client.PostAsApiJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(product.Id, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_Item_Over_Stock_Returns_BadRequest()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var productsResponse = await client.GetAsync("/api/products/?pageSize=1");
        var products =
            await productsResponse.ReadDataAsync<PagedResponse<ProductListItem>>();

        var product = Assert.Single(products.Items);

        var response = await client.PostAsApiJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest(
                product.Id,
                product.StockQuantity + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nonexistent_Cart_Item_Returns_NotFound()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PutAsApiJsonAsync(
            "/api/cart/items/999999999",
            new UpdateCartItemRequest(1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Access_Customer_Cart()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.GetAsync("/api/cart/");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}