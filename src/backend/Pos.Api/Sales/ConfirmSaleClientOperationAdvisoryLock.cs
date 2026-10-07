using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Pos.Api.Sales;

internal static class ConfirmSaleClientOperationAdvisoryLock
{
    private const string AcquireSql = "SELECT pg_advisory_xact_lock(@lock_key);";

    // The caller retains this transaction, including its locks, for the later phases.
    internal static async Task AcquireAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long branchId,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(branchId);
        ArgumentException.ThrowIfNullOrEmpty(clientOperationId);

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

        var lockKey = ConfirmSaleClientOperationAdvisoryKey.Derive(branchId, clientOperationId);
        await using var command = new NpgsqlCommand(AcquireSql, connection, transaction)
        {
            // The barrier intentionally waits; do not impose Npgsql's implicit command deadline.
            CommandTimeout = 0
        };
        command.Parameters.AddWithValue("lock_key", NpgsqlDbType.Bigint, lockKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
