using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class AdminTests
{
    [Fact]
    public async Task Anonymous_User_Cannot_Access_Admin_Endpoints()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData("customer", "Customer123!")]
    [InlineData("merchant", "Merchant123!")]
    [InlineData("service", "Service123!")]
    public async Task Non_Admin_User_Cannot_Access_Admin_Endpoints(
        string username,
        string password)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(username, password);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_Get_User_List()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("admin", "Admin123!");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.ReadDataAsync<IReadOnlyList<UserSummary>>();

        Assert.True(users.Count >= 4);

        Assert.Contains(
            users,
            user =>
                user.Username == "admin" &&
                user.Role == UserRole.Admin);

        Assert.Contains(
            users,
            user =>
                user.Username == "customer" &&
                user.Role == UserRole.Customer);

        Assert.Contains(
            users,
            user =>
                user.Username == "merchant" &&
                user.Role == UserRole.Merchant);

        Assert.Contains(
            users,
            user =>
                user.Username == "service" &&
                user.Role == UserRole.CustomerService);
    }

    [Fact]
    public async Task Admin_Can_Get_All_Orders()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("admin", "Admin123!");

        var response =
            await client.GetAsync("/api/admin/orders");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var orders =
            await response.ReadDataAsync<IReadOnlyList<OrderSummary>>();

        Assert.NotNull(orders);
    }

    [Fact]
    public async Task Admin_Can_Get_Pending_Products()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("admin", "Admin123!");

        var response =
            await client.GetAsync("/api/admin/products/pending");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var products =
            await response.ReadDataAsync<IReadOnlyList<ProductListItem>>();

        Assert.NotNull(products);

        Assert.All(
            products,
            product =>
                Assert.Equal(
                    ProductStatus.PendingReview,
                    product.Status));
    }

    [Fact]
    public async Task Admin_Can_Get_All_Tickets()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("admin", "Admin123!");

        var response =
            await client.GetAsync("/api/admin/tickets");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var tickets =
            await response.ReadDataAsync<IReadOnlyList<TicketResponse>>();

        Assert.NotNull(tickets);
    }

    [Fact]
    public async Task Customer_Cannot_Access_Admin_Orders()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response =
            await client.GetAsync("/api/admin/orders");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Access_Admin_Pending_Products()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response =
            await client.GetAsync("/api/admin/products/pending");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Customer_Service_Cannot_Access_Admin_Tickets()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "service",
            "Service123!");

        var response =
            await client.GetAsync("/api/admin/tickets");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}