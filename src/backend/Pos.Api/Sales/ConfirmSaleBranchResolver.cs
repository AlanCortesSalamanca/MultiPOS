using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Pos.Api.Sales;

internal enum ConfirmSaleBranchResolutionDecision
{
    Resolved,
    ReferenceNotFound
}

internal sealed record ConfirmSaleBranchResolutionResult(
    ConfirmSaleBranchResolutionDecision Decision,
    long? BranchId);

internal static class ConfirmSaleBranchResolver
{
    private const string ResolveSql = """
        SELECT id
        FROM branches
        WHERE business_id = @business_id
          AND public_id = @branch_public_id;
        """;

    // The caller retains its execution connection/transaction; this resolves identity, not activity.
    internal static async Task<ConfirmSaleBranchResolutionResult> ResolveAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long businessId,
        Guid branchPublicId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(businessId);

        if (!ReferenceEquals(transaction.Connection, connection))
        {
            throw new ArgumentException(
                "The confirm sale execution transaction must belong to the supplied connection.",
                nameof(transaction));
        }

        if (transaction.IsolationLevel != IsolationLevel.ReadCommitted)
        {
            throw new ArgumentException(
                "The confirm sale execution transaction requires READ COMMITTED isolation.",
                nameof(transaction));
        }

        await using var command = new NpgsqlCommand(ResolveSql, connection, transaction);
        command.Parameters.AddWithValue("business_id", NpgsqlDbType.Bigint, businessId);
        command.Parameters.AddWithValue("branch_public_id", NpgsqlDbType.Uuid, branchPublicId);

        var branchId = (long?)await command.ExecuteScalarAsync(cancellationToken);
        return new ConfirmSaleBranchResolutionResult(
            branchId is null
                ? ConfirmSaleBranchResolutionDecision.ReferenceNotFound
                : ConfirmSaleBranchResolutionDecision.Resolved,
            branchId);
    }
}
