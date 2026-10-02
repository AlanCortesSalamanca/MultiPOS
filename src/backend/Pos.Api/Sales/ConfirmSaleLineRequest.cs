using System.Text.Json.Serialization;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleLineRequest
{
    [JsonPropertyName("product_public_id")]
    public string? ProductPublicId { get; init; }

    [JsonPropertyName("unit_code")]
    public string? UnitCode { get; init; }

    [JsonPropertyName("unit_context")]
    public string? UnitContext { get; init; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; init; }

    [JsonPropertyName("expected_unit_price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString ExpectedUnitPrice { get; init; }

    [JsonPropertyName("expected_discount_amount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString ExpectedDiscountAmount { get; init; }

    [JsonPropertyName("expected_tax_total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalJsonString ExpectedTaxTotal { get; init; }
}
