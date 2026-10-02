using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pos.Api.Sales;

internal static class ConfirmSaleCanonicalRequestSerializer
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = new CanonicalStringEncoder(),
        Indented = false
    };

    internal static byte[] Serialize(ConfirmSaleRequest request)
    {
        if (request is null ||
            request.Lines is not { Count: > 0 } ||
            request.Payments is not { Count: > 0 })
        {
            throw InvalidRequest();
        }

        var isQuotationSale = request.QuotationPublicId.IsPresent;
        if (isQuotationSale &&
            (request.CustomerPublicId.IsPresent || request.PriceListPublicId.IsPresent))
        {
            throw InvalidRequest();
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

        writer.WriteStartObject();
        writer.WriteString("branch_public_id", RequiredValue(request.BranchPublicId));
        writer.WriteString("cash_session_public_id", RequiredValue(request.CashSessionPublicId));
        writer.WriteString("client_operation_id", RequiredValue(request.ClientOperationId));

        if (request.CustomerPublicId.IsPresent)
        {
            writer.WriteString("customer_public_id", RequiredValue(request.CustomerPublicId));
        }

        writer.WriteStartArray("lines");
        foreach (var line in request.Lines)
        {
            WriteLine(writer, line, isQuotationSale);
        }
        writer.WriteEndArray();

        var payments = new byte[request.Payments.Count][];
        for (var index = 0; index < request.Payments.Count; index++)
        {
            payments[index] = SerializePayment(request.Payments[index]);
        }

        Array.Sort(payments, ComparePayments);
        writer.WriteStartArray("payments");
        foreach (var payment in payments)
        {
            writer.WriteRawValue(payment);
        }
        writer.WriteEndArray();

        if (isQuotationSale)
        {
            writer.WriteString("quotation_public_id", RequiredValue(request.QuotationPublicId));
        }
        else
        {
            writer.WriteString("price_list_public_id", RequiredValue(request.PriceListPublicId));
        }

        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteLine(
        Utf8JsonWriter writer,
        ConfirmSaleLineRequest? line,
        bool isQuotationSale)
    {
        if (line is null ||
            isQuotationSale &&
            (line.ExpectedDiscountAmount.IsPresent ||
             line.ExpectedTaxTotal.IsPresent ||
             line.ExpectedUnitPrice.IsPresent))
        {
            throw InvalidRequest();
        }

        writer.WriteStartObject();
        if (!isQuotationSale)
        {
            writer.WriteString("expected_discount_amount", RequiredValue(line.ExpectedDiscountAmount));
            writer.WriteString("expected_tax_total", RequiredValue(line.ExpectedTaxTotal));
            writer.WriteString("expected_unit_price", RequiredValue(line.ExpectedUnitPrice));
        }

        writer.WriteString("product_public_id", RequiredValue(line.ProductPublicId));
        writer.WriteString("quantity", RequiredValue(line.Quantity));
        writer.WriteString("unit_code", RequiredValue(line.UnitCode));
        writer.WriteString("unit_context", RequiredValue(line.UnitContext));
        writer.WriteEndObject();
    }

    private static byte[] SerializePayment(ConfirmSalePaymentRequest? payment)
    {
        if (payment is null)
        {
            throw InvalidRequest();
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);
        writer.WriteStartObject();
        writer.WriteString("amount", RequiredValue(payment.Amount));
        writer.WriteString("payment_method_code", RequiredValue(payment.PaymentMethodCode));
        if (payment.Reference.IsPresent)
        {
            writer.WriteString("reference", RequiredValue(payment.Reference));
        }
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static int ComparePayments(byte[] left, byte[] right) =>
        left.AsSpan().SequenceCompareTo(right.AsSpan());

    private static string RequiredValue(string? value) => value ?? throw InvalidRequest();

    private static string RequiredValue(OptionalJsonString value) =>
        value.IsPresent ? RequiredValue(value.Value) : throw InvalidRequest();

    private static InvalidOperationException InvalidRequest() =>
        new("Canonical serialization requires a validated confirm sale request.");

    // Built-in JavaScript encoders escape non-BMP Unicode; this encoder only escapes JSON requirements.
    private sealed class CanonicalStringEncoder : JavaScriptEncoder
    {
        public override int MaxOutputCharactersPerInputCharacter =>
            JavaScriptEncoder.UnsafeRelaxedJsonEscaping.MaxOutputCharactersPerInputCharacter;

        public override bool WillEncode(int unicodeScalar) =>
            unicodeScalar is < 0x20 or '"' or '\\';

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            var remaining = new ReadOnlySpan<char>(text, textLength);
            var first = -1;
            var offset = 0;

            while (!remaining.IsEmpty)
            {
                if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done)
                {
                    throw InvalidRequest();
                }

                if (first < 0 && WillEncode(rune.Value))
                {
                    first = offset;
                }

                offset += consumed;
                remaining = remaining[consumed..];
            }

            return first;
        }

        public override unsafe bool TryEncodeUnicodeScalar(
            int unicodeScalar,
            char* buffer,
            int bufferLength,
            out int numberOfCharactersWritten)
        {
            if (!Rune.TryCreate(unicodeScalar, out var rune))
            {
                throw InvalidRequest();
            }

            if (WillEncode(unicodeScalar))
            {
                return JavaScriptEncoder.UnsafeRelaxedJsonEscaping.TryEncodeUnicodeScalar(
                    unicodeScalar,
                    buffer,
                    bufferLength,
                    out numberOfCharactersWritten);
            }

            return rune.TryEncodeToUtf16(
                new Span<char>(buffer, bufferLength),
                out numberOfCharactersWritten);
        }
    }
}
