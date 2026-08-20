using System.Net;
using RetailSystem.Api.Contracts;

namespace RetailSystem.Api.Tests;

public sealed class CartIdValidationTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Update_Cart_Item_With_Invalid_Id_Returns_BadRequest(
        long cartItemId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/cart/items/{cartItemId}",
            new UpdateCartItemRequest(1));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Delete_Cart_Item_With_Invalid_Id_Returns_BadRequest(
        long cartItemId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response = await client.DeleteAsync(
            $"/api/cart/items/{cartItemId}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}