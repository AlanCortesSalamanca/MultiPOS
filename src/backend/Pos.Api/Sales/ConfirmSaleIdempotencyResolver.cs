namespace Pos.Api.Sales;

internal enum ConfirmSaleIdempotencyStatus
{
    InProgress,
    Completed,
    Failed
}

internal enum ConfirmSaleIdempotencyDecision
{
    NewKey,
    KeyReused,
    InProgress,
    CompletedReplay,
    FailedReplay,
    // A non-current lease requires persistent checks; this does not authorize recovery.
    RecoveryCheckRequired
}

internal sealed record ConfirmSaleIdempotencySnapshot(
    string RequestHash,
    ConfirmSaleIdempotencyStatus Status,
    DateTimeOffset? LockedUntil);

internal static class ConfirmSaleIdempotencyResolver
{
    internal static ConfirmSaleIdempotencyDecision Resolve(
        ConfirmSaleIdempotencySnapshot? existing,
        string currentRequestHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentRequestHash);

        if (existing is null)
        {
            return ConfirmSaleIdempotencyDecision.NewKey;
        }

        if (string.IsNullOrEmpty(existing.RequestHash))
        {
            throw new InvalidOperationException(
                "The confirm sale idempotency snapshot requires a request hash.");
        }

        // Hash mismatch takes precedence over status and lease interpretation.
        if (!string.Equals(existing.RequestHash, currentRequestHash, StringComparison.Ordinal))
        {
            return ConfirmSaleIdempotencyDecision.KeyReused;
        }

        switch (existing.Status)
        {
            case ConfirmSaleIdempotencyStatus.Completed:
                return ConfirmSaleIdempotencyDecision.CompletedReplay;

            case ConfirmSaleIdempotencyStatus.Failed:
                return ConfirmSaleIdempotencyDecision.FailedReplay;

            case ConfirmSaleIdempotencyStatus.InProgress:
                var lockedUntil = existing.LockedUntil ?? throw new InvalidOperationException(
                    "An in-progress confirm sale idempotency snapshot requires a lease.");

                return lockedUntil > now
                    ? ConfirmSaleIdempotencyDecision.InProgress
                    : ConfirmSaleIdempotencyDecision.RecoveryCheckRequired;

            default:
                throw new InvalidOperationException(
                    "The confirm sale idempotency snapshot contains an unsupported status.");
        }
    }
}
