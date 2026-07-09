using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Backend3.ApiTests;

public sealed class ApiRouteTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly HttpClient _client;

    public ApiRouteTests(WebApplicationFactory<Api.Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public static TheoryData<string, string> ApiRoutes =>
        new()
        {
            { "POST", "/api/auth/register" },
            { "POST", "/api/auth/login" },
            { "GET", "/api/products" },
            { "GET", "/api/products/test-product" },
            { "POST", "/api/products" },
            { "PUT", "/api/products/test-product/review" },
            { "GET", "/api/categories" },
            { "GET", "/api/orders/cart" },
            { "POST", "/api/orders/cart" },
            { "PUT", "/api/orders/cart/test-cart-item" },
            { "DELETE", "/api/orders/cart/test-cart-item" },
            { "POST", "/api/orders" },
            { "GET", "/api/orders" },
            { "GET", "/api/orders/test-order" },
            { "POST", "/api/orders/pay" },
            { "PUT", "/api/orders/test-order/ship" },
            { "PUT", "/api/orders/test-order/complete" },
            { "PUT", "/api/orders/test-order/cancel" },
            { "POST", "/api/merchants/apply" },
            { "GET", "/api/merchants/pending" },
            { "PUT", "/api/merchants/test-merchant/approve" },
            { "GET", "/api/merchants/my-products" },
            { "GET", "/api/merchants/my-orders" },
            { "GET", "/api/reports/overview" },
            { "GET", "/api/reports/daily-sales" },
            { "GET", "/api/reports/category-sales" },
            { "GET", "/api/reports/merchant" },
            { "POST", "/api/tickets" },
            { "GET", "/api/tickets/my" },
            { "GET", "/api/tickets/assigned" },
            { "PUT", "/api/tickets/test-ticket/reply" },
            { "GET", "/api/tickets/all" }
        };

    [Theory]
    [MemberData(nameof(ApiRoutes))]
    public async Task Api_routes_return_backend3_placeholder_success(string method, string path)
    {
        using var request = CreateRequest(method, path);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("success").GetBoolean());
        Assert.Equal("backend3: 当前接口测试通过", payload.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Object, payload.GetProperty("data").ValueKind);
    }

    [Fact]
    public void Application_entry_point_uses_api_namespace()
    {
        Assert.Equal("Api", typeof(Api.Program).Namespace);
    }

    private static HttpRequestMessage CreateRequest(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return request;
    }
}
