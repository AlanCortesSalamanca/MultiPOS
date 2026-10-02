using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleValidationHttpError(int StatusCode, ApiErrorEnvelope Envelope);

internal static class ConfirmSaleValidationHttpMapper
{
    internal static bool TryMap(
        ConfirmSaleValidationResult validationResult,
        [NotNullWhen(true)] out ConfirmSaleValidationHttpError? httpError)
    {
        httpError = null;

        if (validationResult.IsValid)
        {
            return false;
        }

        var envelope = validationResult.ErrorCode switch
        {
            "REQUEST_VALIDATION_FAILED" => ApiErrorEnvelope.RequestValidationFailed(),
            "INVALID_QUANTITY" => new ApiErrorEnvelope(
                new ApiError(
                    "INVALID_QUANTITY",
                    "The quantity is invalid.",
                    "VALIDATION",
                    false)),
            "INVALID_DISCOUNT" => new ApiErrorEnvelope(
                new ApiError(
                    "INVALID_DISCOUNT",
                    "The discount is invalid.",
                    "VALIDATION",
                    false)),
            _ => throw new InvalidOperationException(
                "The sale validation result contains an unsupported error code.")
        };

        httpError = new ConfirmSaleValidationHttpError(
            StatusCodes.Status422UnprocessableEntity,
            envelope);

        return true;
    }
}
