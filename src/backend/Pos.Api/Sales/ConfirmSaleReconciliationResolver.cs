namespace Pos.Api.Sales;

internal enum ConfirmSaleReconciliationDecision
{
    // No historical sale was found; current sale validations are still required.
    ContinueToSaleValidation,
    ReconcileHistoricalSale
}

internal static class ConfirmSaleReconciliationResolver
{
    // Consumes A2's locked row and A3.5's sale for the exact branch/client-operation pair.
    internal static ConfirmSaleReconciliationDecision Resolve(
        ConfirmSaleIdempotencyExecutionLockResult executionLockResult,
        ConfirmSaleHistoricalSale? historicalSale)
    {
        ArgumentNullException.ThrowIfNull(executionLockResult);

        if (!executionLockResult.RowLockHeld)
        {
            throw new InvalidOperationException(
                "Confirm sale reconciliation requires a held idempotency row lock.");
        }

        if (executionLockResult.Decision is not (
            ConfirmSaleIdempotencyExecutionLockDecision.ContinuationOwned or
            ConfirmSaleIdempotencyExecutionLockDecision.ReconciliationRequired))
        {
            throw new InvalidOperationException(
                "Confirm sale reconciliation requires ContinuationOwned or ReconciliationRequired.");
        }

        if (executionLockResult.Snapshot is null)
        {
            throw new InvalidOperationException(
                "Confirm sale reconciliation requires an authoritative locked idempotency snapshot.");
        }

        if (executionLockResult.ResultEntityId is not { } resultEntityId)
        {
            if (executionLockResult.ResultEntityType is not null)
            {
                throw new InvalidOperationException(
                    "The confirm sale result association contains a type without an ID.");
            }

            if (executionLockResult.Decision == ConfirmSaleIdempotencyExecutionLockDecision.ReconciliationRequired)
            {
                throw new InvalidOperationException(
                    "Confirm sale ReconciliationRequired requires a result entity ID.");
            }

            return historicalSale is null
                ? ConfirmSaleReconciliationDecision.ContinueToSaleValidation
                : ConfirmSaleReconciliationDecision.ReconcileHistoricalSale;
        }

        if (!string.Equals(executionLockResult.ResultEntityType, "sales", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The confirm sale result association requires the exact entity type 'sales'.");
        }

        if (executionLockResult.Decision == ConfirmSaleIdempotencyExecutionLockDecision.ContinuationOwned)
        {
            throw new InvalidOperationException(
                "Confirm sale ContinuationOwned requires no result entity ID.");
        }

        if (historicalSale is null)
        {
            throw new InvalidOperationException(
                "The confirm sale result association requires a corresponding historical sale.");
        }

        if (historicalSale.Id != resultEntityId)
        {
            throw new InvalidOperationException(
                "The confirm sale result association does not match the historical sale ID.");
        }

        return ConfirmSaleReconciliationDecision.ReconcileHistoricalSale;
    }
}
