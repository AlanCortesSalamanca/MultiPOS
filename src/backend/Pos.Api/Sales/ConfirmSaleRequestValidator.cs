using System.Buffers;
using System.Globalization;
using System.Text;

namespace Pos.Api.Sales;

internal readonly record struct ConfirmSaleValidationResult(bool IsValid, string? ErrorCode);

internal static class ConfirmSaleRequestValidator
{
    private const string RequestValidationFailed = "REQUEST_VALIDATION_FAILED";
    private const string InvalidQuantity = "INVALID_QUANTITY";
    private const string InvalidDiscount = "INVALID_DISCOUNT";

    internal static ConfirmSaleValidationResult Validate(ConfirmSaleRequest? request)
    {
        if (request is null ||
            string.IsNullOrEmpty(request.BranchPublicId) ||
            string.IsNullOrEmpty(request.CashSessionPublicId) ||
            !IsValidClientOperationId(request.ClientOperationId))
        {
            return Invalid(RequestValidationFailed);
        }

        var isQuotationSale = request.QuotationPublicId.IsPresent;
        if (isQuotationSale)
        {
            if (string.IsNullOrEmpty(request.QuotationPublicId.Value) ||
                request.CustomerPublicId.IsPresent ||
                request.PriceListPublicId.IsPresent)
            {
                return Invalid(RequestValidationFailed);
            }
        }
        else if (!request.PriceListPublicId.IsPresent ||
                 string.IsNullOrEmpty(request.PriceListPublicId.Value) ||
                 request.CustomerPublicId is { IsPresent: true, Value: null or "" })
        {
            return Invalid(RequestValidationFailed);
        }

        if (request.Lines is not { Count: > 0 })
        {
            return Invalid(RequestValidationFailed);
        }

        foreach (var line in request.Lines)
        {
            var lineResult = ValidateLine(line, isQuotationSale);
            if (!lineResult.IsValid)
            {
                return lineResult;
            }
        }

        if (request.Payments is not { Count: > 0 })
        {
            return Invalid(RequestValidationFailed);
        }

        foreach (var payment in request.Payments)
        {
            if (payment is null ||
                string.IsNullOrEmpty(payment.PaymentMethodCode) ||
                payment.Amount is null ||
                !TryGetCanonicalDecimalSign(payment.Amount, 2, out var amountSign) ||
                amountSign <= 0 ||
                payment.Reference is { IsPresent: true, Value: null })
            {
                return Invalid(RequestValidationFailed);
            }
        }

        return new ConfirmSaleValidationResult(true, null);
    }

    private static ConfirmSaleValidationResult ValidateLine(
        ConfirmSaleLineRequest? line,
        bool isQuotationSale)
    {
        if (line is null ||
            string.IsNullOrEmpty(line.ProductPublicId) ||
            string.IsNullOrEmpty(line.UnitCode) ||
            line.UnitContext is not ("SALE" or "BOTH") ||
            line.Quantity is null)
        {
            return Invalid(RequestValidationFailed);
        }

        if (!TryGetCanonicalDecimalSign(line.Quantity, 4, out var quantitySign) ||
            quantitySign <= 0)
        {
            return Invalid(InvalidQuantity);
        }

        if (isQuotationSale)
        {
            return line.ExpectedUnitPrice.IsPresent ||
                   line.ExpectedDiscountAmount.IsPresent ||
                   line.ExpectedTaxTotal.IsPresent
                ? Invalid(RequestValidationFailed)
                : new ConfirmSaleValidationResult(true, null);
        }

        if (!line.ExpectedUnitPrice.IsPresent ||
            line.ExpectedUnitPrice.Value is null ||
            !line.ExpectedDiscountAmount.IsPresent ||
            line.ExpectedDiscountAmount.Value is null ||
            !line.ExpectedTaxTotal.IsPresent ||
            line.ExpectedTaxTotal.Value is null)
        {
            return Invalid(RequestValidationFailed);
        }

        if (!TryGetCanonicalDecimalSign(
                line.ExpectedUnitPrice.Value,
                2,
                out var unitPriceSign) ||
            unitPriceSign < 0)
        {
            return Invalid(RequestValidationFailed);
        }

        if (!TryGetCanonicalDecimalSign(
                line.ExpectedDiscountAmount.Value,
                2,
                out var discountSign))
        {
            return Invalid(RequestValidationFailed);
        }

        if (discountSign < 0)
        {
            return Invalid(InvalidDiscount);
        }

        if (!TryGetCanonicalDecimalSign(
                line.ExpectedTaxTotal.Value,
                2,
                out var taxSign) ||
            taxSign < 0)
        {
            return Invalid(RequestValidationFailed);
        }

        return new ConfirmSaleValidationResult(true, null);
    }

    private static bool IsValidClientOperationId(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var remaining = value.AsSpan();
        var runeCount = 0;

        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(
                    remaining,
                    out var rune,
                    out var charactersConsumed) != OperationStatus.Done ||
                Rune.GetUnicodeCategory(rune) == UnicodeCategory.Control ||
                ++runeCount > 128)
            {
                return false;
            }

            remaining = remaining[charactersConsumed..];
        }

        return true;
    }

    private static bool TryGetCanonicalDecimalSign(
        string value,
        int scale,
        out int sign)
    {
        sign = 0;

        var digitsStart = value.StartsWith('-') ? 1 : 0;
        var decimalPointIndex = value.Length - scale - 1;

        if (decimalPointIndex <= digitsStart ||
            value[decimalPointIndex] != '.' ||
            decimalPointIndex - digitsStart > 1 && value[digitsStart] == '0')
        {
            return false;
        }

        var isZero = true;

        for (var index = digitsStart; index < value.Length; index++)
        {
            if (index == decimalPointIndex)
            {
                continue;
            }

            var character = value[index];
            if (character is < '0' or > '9')
            {
                return false;
            }

            isZero &= character == '0';
        }

        if (digitsStart == 1 && isZero)
        {
            return false;
        }

        sign = isZero ? 0 : digitsStart == 1 ? -1 : 1;
        return true;
    }

    private static ConfirmSaleValidationResult Invalid(string errorCode) =>
        new(false, errorCode);
}
