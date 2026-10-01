namespace Pos.Api;

internal sealed record ApiErrorEnvelope(ApiError Error);

internal sealed record ApiError(
    string Code,
    string Message,
    string Category,
    bool Retryable);
