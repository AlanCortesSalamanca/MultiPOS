using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pos.Api.Sales;

internal static class ConfirmSaleReconciliationResponseBodySerializer
{
    private const int MaximumResponseBodyBytes = 16 * 1024;

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false
    };

    // The caller has already obtained ReconcileHistoricalSale from A3.6.2.
    internal static string Serialize(ConfirmSaleHistoricalSale historicalSale)
    {
        ArgumentNullException.ThrowIfNull(historicalSale);

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        writer.WriteStartObject();
        writer.WriteString("sale_public_id", historicalSale.PublicId.ToString("D"));
        writer.WriteString("folio", historicalSale.Folio);
        writer.WriteString("status", "CONFIRMED");
        writer.WriteString("total", historicalSale.Total.ToString("0.00", CultureInfo.InvariantCulture));
        writer.WriteString("currency", historicalSale.Currency);
        writer.WriteString(
            "confirmed_at",
            historicalSale.ConfirmedAt.ToUniversalTime().ToString(
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                CultureInfo.InvariantCulture));
        writer.WriteEndObject();
        writer.Flush();

        if (buffer.WrittenCount > MaximumResponseBodyBytes)
        {
            throw new InvalidOperationException(
                "The confirm sale reconciliation response body exceeds the 16 KiB UTF-8 limit.");
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
