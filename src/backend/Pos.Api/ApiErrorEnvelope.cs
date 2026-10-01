namespace Pos.Api;

internal sealed record ApiErrorEnvelope(ApiError Error)
{
    internal static ApiErrorEnvelope RequestValidationFailed() => new(
        new ApiError(
            "REQUEST_VALIDATION_FAILED",
            "The request is invalid.",
            "VALIDATION",
            false));
}

internal sealed record ApiError(
    string Code,
    string Message,
    string Category,
    bool Retryable);
