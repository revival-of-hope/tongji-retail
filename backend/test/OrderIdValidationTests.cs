using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class OrderIdValidationTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Get_Order_With_Invalid_Id_Returns_BadRequest(long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.GetAsync(
            $"/api/orders/{orderId}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Pay_Order_With_Invalid_Id_Returns_BadRequest(long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            $"/api/orders/{orderId}/pay",
            new PayOrderRequest(PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Ship_Order_With_Invalid_Id_Returns_BadRequest(long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.PutAsync(
            $"/api/orders/{orderId}/ship",
            null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Complete_Order_With_Invalid_Id_Returns_BadRequest(long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PutAsync(
            $"/api/orders/{orderId}/complete",
            null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Cancel_Order_With_Invalid_Id_Returns_BadRequest(long orderId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PutAsync(
            $"/api/orders/{orderId}/cancel",
            null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}