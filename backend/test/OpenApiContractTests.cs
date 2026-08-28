using System.Text.Json;
using System.Text.Json.Nodes;

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
    public async Task Runtime_OpenApi_Contract_Matches_Checked_In_Contract()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        var runtimeJson = await client.GetStringAsync("/openapi/v1.json");
        var checkedInJson = await File.ReadAllTextAsync(CheckedInContractPath());

        Assert.Equal(Normalize(runtimeJson), Normalize(checkedInJson));
    }

    private static JsonDocument ReadCheckedInContract() =>
        JsonDocument.Parse(File.ReadAllText(CheckedInContractPath()));

    private static string CheckedInContractPath() =>
        Path.Combine(AppContext.BaseDirectory, "openapi", "retail-system.json");

    private static string Normalize(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("OpenAPI document is empty");

        // The WebApplicationFactory runtime host and the checked-in build
        // contract can expose different server URLs. Paths, schemas, request
        // bodies, responses and security metadata must otherwise be identical.
        root.Remove("servers");
        return SortNode(root)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonNode? SortNode(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => KeyValuePair.Create(property.Key, SortNode(property.Value)))),
        JsonArray array => new JsonArray(array.Select(SortNode).ToArray()),
        null => null,
        _ => node.DeepClone()
    };

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
