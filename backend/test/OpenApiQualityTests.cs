using System.Text.Json;
using System.Text.RegularExpressions;

namespace RetailSystem.Api.Tests;

public sealed class OpenApiQualityTests
{
    private static readonly HashSet<string> HttpMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "get",
            "post",
            "put",
            "delete",
            "patch",
            "head",
            "options"
        };

    private static readonly Regex PathParameterPattern =
        new(
            @"\{(?<name>[^{}]+)\}",
            RegexOptions.Compiled);

    [Fact]
    public void Every_Operation_Has_Required_Metadata()
    {
        using var document = ReadCheckedInContract();

        var operations =
            Operations(document.RootElement).ToArray();

        Assert.NotEmpty(operations);

        foreach (var operation in operations)
        {
            Assert.True(
                operation.Operation.TryGetProperty(
                    "operationId",
                    out var operationId) &&
                !string.IsNullOrWhiteSpace(
                    operationId.GetString()),
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 缺少 operationId");

            Assert.True(
                operation.Operation.TryGetProperty(
                    "summary",
                    out var summary) &&
                !string.IsNullOrWhiteSpace(
                    summary.GetString()),
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 缺少 summary");

            Assert.True(
                operation.Operation.TryGetProperty(
                    "tags",
                    out var tags) &&
                tags.ValueKind == JsonValueKind.Array &&
                tags.GetArrayLength() > 0,
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 缺少 tags");

            foreach (var tag in tags.EnumerateArray())
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        tag.GetString()),
                    $"{operation.Method.ToUpperInvariant()} " +
                    $"{operation.Path} 包含空 tag");
            }
        }
    }

    [Fact]
    public void Every_Operation_Defines_A_Success_Response()
    {
        using var document = ReadCheckedInContract();

        var operations =
            Operations(document.RootElement).ToArray();

        Assert.NotEmpty(operations);

        foreach (var operation in operations)
        {
            Assert.True(
                operation.Operation.TryGetProperty(
                    "responses",
                    out var responses),
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 缺少 responses");

            var hasSuccessResponse =
                responses
                    .EnumerateObject()
                    .Any(response =>
                        int.TryParse(
                            response.Name,
                            out var statusCode) &&
                        statusCode >= 200 &&
                        statusCode < 300);

            Assert.True(
                hasSuccessResponse,
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 没有定义 2xx 成功响应");
        }
    }

    [Fact]
    public void Every_Path_Template_Parameter_Is_Declared_And_Required()
    {
        using var document = ReadCheckedInContract();

        foreach (var operation in
                 Operations(document.RootElement))
        {
            var expectedParameters =
                PathParameterPattern
                    .Matches(operation.Path)
                    .Select(match =>
                        match.Groups["name"].Value)
                    .ToHashSet(StringComparer.Ordinal);

            if (expectedParameters.Count == 0)
            {
                continue;
            }

            var declaredParameters =
                CollectPathParameters(
                    document.RootElement,
                    operation.PathItem,
                    operation.Operation);

            var missingParameters =
                expectedParameters
                    .Except(
                        declaredParameters.Keys,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.True(
                missingParameters.Length == 0,
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 缺少 path 参数声明: " +
                $"{string.Join(", ", missingParameters)}");

            var unexpectedParameters =
                declaredParameters.Keys
                    .Except(
                        expectedParameters,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.True(
                unexpectedParameters.Length == 0,
                $"{operation.Method.ToUpperInvariant()} " +
                $"{operation.Path} 存在多余 path 参数: " +
                $"{string.Join(", ", unexpectedParameters)}");

            foreach (var parameterName in expectedParameters)
            {
                var parameter =
                    declaredParameters[parameterName];

                Assert.True(
                    parameter.TryGetProperty(
                        "required",
                        out var required) &&
                    required.ValueKind ==
                        JsonValueKind.True,
                    $"{operation.Method.ToUpperInvariant()} " +
                    $"{operation.Path} 的 path 参数 " +
                    $"{parameterName} 必须 required=true");
            }
        }
    }

    [Fact]
    public void All_Local_OpenApi_References_Can_Be_Resolved()
    {
        using var document = ReadCheckedInContract();

        var references =
            LocalReferences(document.RootElement)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        Assert.NotEmpty(references);

        foreach (var reference in references)
        {
            Assert.True(
                TryResolveLocalReference(
                    document.RootElement,
                    reference,
                    out _),
                $"OpenAPI 中存在无法解析的引用: {reference}");
        }
    }

    [Fact]
    public void Security_Requirements_Reference_Defined_Schemes()
    {
        using var document = ReadCheckedInContract();

        var root = document.RootElement;

        var components =
            root.GetProperty("components");

        Assert.True(
            components.TryGetProperty(
                "securitySchemes",
                out var securitySchemes),
            "OpenAPI components 中缺少 securitySchemes");

        var usedSchemes =
            new HashSet<string>(
                StringComparer.Ordinal);

        void ValidateSecurity(
            JsonElement owner,
            string context)
        {
            if (!owner.TryGetProperty(
                    "security",
                    out var security) ||
                security.ValueKind !=
                    JsonValueKind.Array)
            {
                return;
            }

            foreach (var requirement
                     in security.EnumerateArray())
            {
                foreach (var scheme
                         in requirement.EnumerateObject())
                {
                    usedSchemes.Add(scheme.Name);

                    Assert.True(
                        securitySchemes.TryGetProperty(
                            scheme.Name,
                            out _),
                        $"{context} 使用了未定义的 " +
                        $"security scheme: {scheme.Name}");
                }
            }
        }

        ValidateSecurity(root, "OpenAPI root");

        foreach (var operation in Operations(root))
        {
            ValidateSecurity(
                operation.Operation,
                $"{operation.Method.ToUpperInvariant()} " +
                operation.Path);
        }

        Assert.NotEmpty(usedSchemes);
    }

    [Fact]
    public void Envelope_Schemas_Use_Standard_Response_Shape()
    {
        using var document = ReadCheckedInContract();

        var schemas =
            document.RootElement
                .GetProperty("components")
                .GetProperty("schemas");

        var envelopes =
            schemas
                .EnumerateObject()
                .Where(schema =>
                    schema.Name.EndsWith(
                        "Envelope",
                        StringComparison.Ordinal))
                .ToArray();

        Assert.NotEmpty(envelopes);

        foreach (var envelope in envelopes)
        {
            Assert.True(
                envelope.Value.TryGetProperty(
                    "properties",
                    out var properties),
                $"{envelope.Name} 缺少 properties");

            Assert.True(
                properties.TryGetProperty(
                    "code",
                    out var code),
                $"{envelope.Name} 缺少 code");

            Assert.True(
                properties.TryGetProperty(
                    "message",
                    out var message),
                $"{envelope.Name} 缺少 message");

            Assert.True(
                properties.TryGetProperty(
                    "data",
                    out _),
                $"{envelope.Name} 缺少 data");

            Assert.Equal(
                "integer",
                code.GetProperty("type").GetString());

            Assert.Equal(
                "string",
                message.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void Core_Backend_Contracts_Are_Published()
    {
        using var document = ReadCheckedInContract();

        var schemas =
            document.RootElement
                .GetProperty("components")
                .GetProperty("schemas");

        string[] expectedSchemas =
        [
            "RegisterRequest",
            "LoginRequest",
            "UserSummary",

            "ApplyMerchantRequest",
            "MerchantSummary",
            "ReviewMerchantRequest",

            "ProductListItem",
            "ProductDetail",
            "CreateProductRequest",
            "UpdateProductRequest",

            "AddCartItemRequest",
            "UpdateCartItemRequest",
            "CartItemResponse",

            "CreateOrderRequest",
            "OrderSummary",
            "OrderDetail",

            "CreateTicketRequest",
            "TicketResponse",

            "OverviewReport",
            "DailySalesPoint",
            "CategorySalesPoint",
            "MerchantReport"
        ];

        foreach (var schemaName in expectedSchemas)
        {
            Assert.True(
                schemas.TryGetProperty(
                    schemaName,
                    out _),
                $"OpenAPI components/schemas " +
                $"中缺少核心 Contract: {schemaName}");
        }
    }

    private static JsonDocument ReadCheckedInContract()
    {
        return JsonDocument.Parse(
            File.ReadAllText(
                CheckedInContractPath()));
    }

    private static string CheckedInContractPath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "openapi",
            "retail-system.json");
    }

    private static IEnumerable<(
        string Path,
        string Method,
        JsonElement PathItem,
        JsonElement Operation)> Operations(
            JsonElement root)
    {
        foreach (var route in
                 root.GetProperty("paths")
                     .EnumerateObject())
        {
            foreach (var method in
                     route.Value.EnumerateObject())
            {
                if (!HttpMethods.Contains(
                        method.Name))
                {
                    continue;
                }

                yield return (
                    route.Name,
                    method.Name,
                    route.Value,
                    method.Value);
            }
        }
    }

    private static Dictionary<string, JsonElement>
        CollectPathParameters(
            JsonElement root,
            JsonElement pathItem,
            JsonElement operation)
    {
        var result =
            new Dictionary<string, JsonElement>(
                StringComparer.Ordinal);

        AddParameters(pathItem);
        AddParameters(operation);

        return result;

        void AddParameters(JsonElement owner)
        {
            if (!owner.TryGetProperty(
                    "parameters",
                    out var parameters) ||
                parameters.ValueKind !=
                    JsonValueKind.Array)
            {
                return;
            }

            foreach (var rawParameter
                     in parameters.EnumerateArray())
            {
                var parameter =
                    ResolveReferenceIfNeeded(
                        root,
                        rawParameter);

                if (!parameter.TryGetProperty(
                        "in",
                        out var parameterLocation) ||
                    parameterLocation.GetString() !=
                        "path")
                {
                    continue;
                }

                if (!parameter.TryGetProperty(
                        "name",
                        out var parameterName))
                {
                    continue;
                }

                var name =
                    parameterName.GetString();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                result[name] = parameter;
            }
        }
    }

    private static JsonElement ResolveReferenceIfNeeded(
        JsonElement root,
        JsonElement element)
    {
        if (element.ValueKind ==
                JsonValueKind.Object &&
            element.TryGetProperty(
                "$ref",
                out var referenceElement) &&
            referenceElement.GetString()
                is { } reference &&
            reference.StartsWith(
                "#/",
                StringComparison.Ordinal) &&
            TryResolveLocalReference(
                root,
                reference,
                out var resolved))
        {
            return resolved;
        }

        return element;
    }

    private static IEnumerable<string>
        LocalReferences(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property
                         in element.EnumerateObject())
                {
                    if (property.NameEquals("$ref") &&
                        property.Value.ValueKind ==
                            JsonValueKind.String &&
                        property.Value.GetString()
                            is { } reference &&
                        reference.StartsWith(
                            "#/",
                            StringComparison.Ordinal))
                    {
                        yield return reference;
                    }

                    foreach (var nestedReference
                             in LocalReferences(
                                 property.Value))
                    {
                        yield return nestedReference;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item
                         in element.EnumerateArray())
                {
                    foreach (var nestedReference
                             in LocalReferences(item))
                    {
                        yield return nestedReference;
                    }
                }

                break;
        }
    }

    private static bool TryResolveLocalReference(
        JsonElement root,
        string reference,
        out JsonElement resolved)
    {
        resolved = default;

        if (!reference.StartsWith(
                "#/",
                StringComparison.Ordinal))
        {
            return false;
        }

        var current = root;

        foreach (var rawSegment in
                 reference[2..].Split('/'))
        {
            var segment =
                DecodeJsonPointerSegment(
                    rawSegment);

            if (current.ValueKind ==
                JsonValueKind.Object)
            {
                if (!current.TryGetProperty(
                        segment,
                        out var child))
                {
                    return false;
                }

                current = child;
                continue;
            }

            if (current.ValueKind ==
                    JsonValueKind.Array &&
                int.TryParse(
                    segment,
                    out var index) &&
                index >= 0 &&
                index < current.GetArrayLength())
            {
                current = current[index];
                continue;
            }

            return false;
        }

        resolved = current;
        return true;
    }

    private static string DecodeJsonPointerSegment(
        string segment)
    {
        return segment
            .Replace(
                "~1",
                "/",
                StringComparison.Ordinal)
            .Replace(
                "~0",
                "~",
                StringComparison.Ordinal);
    }
}