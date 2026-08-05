using System.Text.Json;
using System.Text.Json.Serialization;

namespace RetailSystem.Api.Serialization;

/// <summary>
/// Serializes every API <see cref="DateTime"/> value as UTC.
/// Oracle DATE/TIMESTAMP columns do not preserve <see cref="DateTime.Kind"/>,
/// so values read back from Oracle are commonly marked as Unspecified even
/// though this application stores them as UTC.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !reader.TryGetDateTime(out var value))
            throw new JsonException("日期时间必须是有效的 ISO 8601 字符串");

        return NormalizeToUtc(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(NormalizeToUtc(value));

    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),

        // The project writes UTC timestamps to Oracle. Oracle returns the same
        // clock value without DateTime.Kind, so restore the missing UTC marker
        // instead of treating the value as server-local time.
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "未知的日期时间类型")
    };
}
