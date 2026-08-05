using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RetailSystem.Api.Contracts;

namespace RetailSystem.Api.Tests;

internal static class ApiTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<T> ReadDataAsync<T>(this HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Data);
        return envelope.Data!;
    }

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.ReadDataAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }
}
