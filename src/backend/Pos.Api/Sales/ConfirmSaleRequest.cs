using System.Text.Json.Serialization;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleRequest
{
    [JsonPropertyName("branch_public_id")]
    public string? BranchPublicId { get; init; }

    [JsonPropertyName("cash_session_public_id")]
    public string? CashSessionPublicId { get; init; }

    [JsonPropertyName("client_operation_id")]
    public string? ClientOperationId { get; init; }

    [JsonPropertyName("customer_public_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString CustomerPublicId { get; init; }

    [JsonPropertyName("price_list_public_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString PriceListPublicId { get; init; }

    [JsonPropertyName("quotation_public_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString QuotationPublicId { get; init; }

    [JsonPropertyName("lines")]
    public IReadOnlyList<ConfirmSaleLineRequest>? Lines { get; init; }

    [JsonPropertyName("payments")]
    public IReadOnlyList<ConfirmSalePaymentRequest>? Payments { get; init; }
}
