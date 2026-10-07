using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleHistoricalSale(
    long Id,
    Guid PublicId,
    string Folio,
    decimal Total,
    string Currency,
    DateTimeOffset ConfirmedAt);

internal static class ConfirmSaleHistoricalSaleLookup
{
    private const string FindSql = """
        SELECT id, public_id, folio, total, currency, confirmed_at
        FROM sales
        WHERE branch_id = @branch_id
          AND client_operation_id = @client_operation_id
        FOR UPDATE;
        """;

    // The caller has already acquired the advisory barrier and retains this transaction's row lock.
    internal static async Task<ConfirmSaleHistoricalSale?> FindAsync(
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

        await using var command = new NpgsqlCommand(FindSql, connection, transaction);
        command.Parameters.AddWithValue("branch_id", NpgsqlDbType.Bigint, branchId);
        command.Parameters.AddWithValue("client_operation_id", NpgsqlDbType.Text, clientOperationId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ConfirmSaleHistoricalSale(
            reader.GetInt64(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetDecimal(3),
            reader.GetString(4),
            reader.GetFieldValue<DateTimeOffset>(5));
    }
}
