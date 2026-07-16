using System.Text.Json;

namespace RetailSystem.Api.Tests;

public sealed class OpenApiContractTests
{
    [Fact]
    public void Checked_In_OpenApi_Contract_Has_Unique_OperationIds()
    {
        using var document = ReadCheckedInContract();
        var ids = OperationIds(document.RootElement);

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Runtime_OperationIds_Match_Checked_In_OpenApi_Contract()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        var runtimeJson = await client.GetStringAsync("/openapi/v1.json");
        using var runtime = JsonDocument.Parse(runtimeJson);
        using var checkedIn = ReadCheckedInContract();

        Assert.Equal(
            OperationIds(checkedIn.RootElement).OrderBy(x => x, StringComparer.Ordinal),
            OperationIds(runtime.RootElement).OrderBy(x => x, StringComparer.Ordinal));
    }

    private static JsonDocument ReadCheckedInContract()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "openapi", "retail-system.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static List<string> OperationIds(JsonElement root)
    {
        var ids = new List<string>();
        foreach (var route in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in route.Value.EnumerateObject())
            {
                if (operation.Value.TryGetProperty("operationId", out var id) && id.GetString() is { } value)
                    ids.Add(value);
            }
        }
        return ids;
    }
}
