using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pos.Api.Sales;

[JsonConverter(typeof(OptionalJsonStringConverter))]
internal readonly record struct OptionalJsonString
{
    private OptionalJsonString(bool isPresent, string? value)
    {
        IsPresent = isPresent;
        Value = value;
    }

    public bool IsPresent { get; }

    public string? Value { get; }

    internal static OptionalJsonString Present(string? value) => new(true, value);
}

internal sealed class OptionalJsonStringConverter : JsonConverter<OptionalJsonString>
{
    public override bool HandleNull => true;

    public override OptionalJsonString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return OptionalJsonString.Present(null);
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a JSON string or null.");
        }

        return OptionalJsonString.Present(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        OptionalJsonString value,
        JsonSerializerOptions options)
    {
        if (value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
