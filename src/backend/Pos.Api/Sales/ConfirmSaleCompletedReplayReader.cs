using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pos.Api.Sales;

internal static class ConfirmSaleCompletedReplayReader
{
    private const int MaximumResponseBodyBytes = 16 * 1024;
    private const int PropertyCount = 6;
    private const int AllPropertiesSeen = (1 << PropertyCount) - 1;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false
    };

    // The caller must compare the request hash and select COMPLETED before reading the body.
    internal static ConfirmSaleCompletedReplayResult Read(
        string? resultEntityType,
        long? resultEntityId,
        string? responseBody)
    {
        if (!string.Equals(resultEntityType, "sales", StringComparison.Ordinal) ||
            resultEntityId is not > 0)
        {
            throw new InvalidOperationException(
                "The confirm sale completed replay result association is invalid.");
        }

        if (responseBody is null)
        {
            throw InvalidResponseBody();
        }

        try
        {
            return ReadResponseBody(responseBody);
        }
        catch (Exception exception) when (
            exception is JsonException or EncoderFallbackException or InvalidOperationException)
        {
            // Do not retain parser messages or inner exceptions containing historical data.
            throw InvalidResponseBody();
        }
    }

    private static ConfirmSaleCompletedReplayResult ReadResponseBody(string responseBody)
    {
        // Reject raw unpaired UTF-16 surrogates instead of allowing replacement during transcoding.
        var reader = new Utf8JsonReader(StrictUtf8.GetBytes(responseBody));
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw InvalidResponseBody();
        }

        var values = new string[PropertyCount];
        var seen = 0;

        // Enumerate every property occurrence, including duplicates with escaped names.
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyIndex = ReadUnicodeString(ref reader) switch
            {
                "sale_public_id" => 0,
                "folio" => 1,
                "status" => 2,
                "total" => 3,
                "currency" => 4,
                "confirmed_at" => 5,
                _ => throw InvalidResponseBody()
            };

            var propertyBit = 1 << propertyIndex;
            if ((seen & propertyBit) != 0 ||
                !reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw InvalidResponseBody();
            }

            values[propertyIndex] = ReadUnicodeString(ref reader);
            seen |= propertyBit;
        }

        if (reader.TokenType != JsonTokenType.EndObject ||
            seen != AllPropertiesSeen || reader.Read())
        {
            throw InvalidResponseBody();
        }

        if (!TryReadSalePublicId(values[0], out var salePublicId) ||
            !string.Equals(values[2], "CONFIRMED", StringComparison.Ordinal) ||
            !IsValidTotal(values[3]) ||
            !IsValidCurrency(values[4]) ||
            !IsValidConfirmedAt(values[5]))
        {
            throw InvalidResponseBody();
        }

        EnsureResponseBodySize(values);

        return new ConfirmSaleCompletedReplayResult(
            salePublicId,
            values[1],
            values[2],
            values[3],
            values[4],
            values[5]);
    }

    private static string ReadUnicodeString(ref Utf8JsonReader reader)
    {
        // Read() has checked JSON escape syntax; check surrogate pairing before GetString().
        var token = reader.ValueSpan;
        for (var index = 0; index < token.Length; index++)
        {
            if (token[index] != (byte)'\\')
            {
                continue;
            }

            if (token[++index] != (byte)'u')
            {
                continue;
            }

            var codeUnit = ReadHexCodeUnit(token.Slice(index + 1, 4));
            index += 4;

            if (codeUnit is >= 0xDC00 and <= 0xDFFF)
            {
                throw InvalidResponseBody();
            }

            if (codeUnit is < 0xD800 or > 0xDBFF)
            {
                continue;
            }

            if (index + 6 >= token.Length ||
                token[index + 1] != (byte)'\\' || token[index + 2] != (byte)'u')
            {
                throw InvalidResponseBody();
            }

            var lowSurrogate = ReadHexCodeUnit(token.Slice(index + 3, 4));
            if (lowSurrogate is < 0xDC00 or > 0xDFFF)
            {
                throw InvalidResponseBody();
            }

            index += 6;
        }

        return reader.GetString() ?? throw InvalidResponseBody();
    }

    private static int ReadHexCodeUnit(ReadOnlySpan<byte> digits)
    {
        var value = 0;
        foreach (var digit in digits)
        {
            var hexDigit = digit switch
            {
                >= (byte)'0' and <= (byte)'9' => digit - (byte)'0',
                >= (byte)'A' and <= (byte)'F' => digit - (byte)'A' + 10,
                >= (byte)'a' and <= (byte)'f' => digit - (byte)'a' + 10,
                _ => throw InvalidResponseBody()
            };

            value = (value << 4) | hexDigit;
        }

        return value;
    }

    private static bool TryReadSalePublicId(string value, out Guid salePublicId)
    {
        salePublicId = Guid.Empty;
        if (value.Length != 36)
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (index is 8 or 13 or 18 or 23)
            {
                if (value[index] != '-')
                {
                    return false;
                }
            }
            else if (value[index] is not (>= '0' and <= '9' or
                                         >= 'A' and <= 'F' or
                                         >= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return Guid.TryParseExact(value, "D", out salePublicId);
    }

    private static bool IsValidTotal(string value)
    {
        var integerDigits = value.Length - 3;
        if (integerDigits is < 1 or > 16 ||
            value[integerDigits] != '.' ||
            integerDigits > 1 && value[0] == '0')
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (index != integerDigits && value[index] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidCurrency(string value) =>
        value.Length == 3 &&
        value[0] is >= 'A' and <= 'Z' &&
        value[1] is >= 'A' and <= 'Z' &&
        value[2] is >= 'A' and <= 'Z';

    private static bool IsValidConfirmedAt(string value)
    {
        if (value.Length != 20 && value.Length is not (>= 22 and <= 28))
        {
            return false;
        }

        if (value[4] != '-' || value[7] != '-' || value[10] != 'T' ||
            value[13] != ':' || value[16] != ':' || value[^1] != 'Z' ||
            value.Length > 20 && value[19] != '.')
        {
            return false;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (index is 4 or 7 or 10 or 13 or 16 ||
                index == value.Length - 1 || index == 19 && value.Length > 20)
            {
                continue;
            }

            if (value[index] is < '0' or > '9')
            {
                return false;
            }
        }

        var format = value.Length == 20
            ? "yyyy-MM-dd'T'HH:mm:ss'Z'"
            : "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

        return DateTimeOffset.TryParseExact(
            value,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _);
    }

    private static void EnsureResponseBodySize(string[] values)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        // Measure the serializer-equivalent compact body, never raw JSONB text or reformatted values.
        writer.WriteStartObject();
        writer.WriteString("sale_public_id", values[0]);
        writer.WriteString("folio", values[1]);
        writer.WriteString("status", values[2]);
        writer.WriteString("total", values[3]);
        writer.WriteString("currency", values[4]);
        writer.WriteString("confirmed_at", values[5]);
        writer.WriteEndObject();
        writer.Flush();

        if (buffer.WrittenCount > MaximumResponseBodyBytes)
        {
            throw InvalidResponseBody();
        }
    }

    private static InvalidOperationException InvalidResponseBody() =>
        new("The confirm sale completed replay response body is invalid.");
}

internal sealed record ConfirmSaleCompletedReplayResult(
    Guid SalePublicId,
    string Folio,
    string Status,
    string Total,
    string Currency,
    string ConfirmedAt);
