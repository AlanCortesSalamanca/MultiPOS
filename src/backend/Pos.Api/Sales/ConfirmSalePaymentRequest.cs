using System.Text.Json.Serialization;

namespace Pos.Api.Sales;

internal sealed record ConfirmSalePaymentRequest
{
    [JsonPropertyName("payment_method_code")]
    public string? PaymentMethodCode { get; init; }

    [JsonPropertyName("amount")]
    public string? Amount { get; init; }

    [JsonPropertyName("reference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString Reference { get; init; }
}
