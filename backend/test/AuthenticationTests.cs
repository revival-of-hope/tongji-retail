using System.Net;
using System.Net.Http.Json;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task Register_Then_Get_Current_User_Succeeds()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}"[..20];

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(username, "Password123!", $"{username}@example.com", null));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var auth = await register.ReadDataAsync<AuthResponse>();
        Assert.Equal(UserRole.Customer, auth.User.Role);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var currentUser = await me.ReadDataAsync<UserSummary>();
        Assert.Equal(username, currentUser.Username);
    }

    [Fact]
    public async Task Protected_Endpoint_Rejects_Anonymous_Request()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cart/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
