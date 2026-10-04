using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleIdempotencyPhaseAResult(
    long Id,
    ConfirmSaleIdempotencyDecision Decision,
    ConfirmSaleIdempotencySnapshot Snapshot);

internal sealed class ConfirmSaleIdempotencyStore
{
    private const string OperationType = "CONFIRM_SALE";

    private const string ReserveSql = """
        INSERT INTO idempotency_keys (
            business_id,
            branch_id,
            operation_type,
            idempotency_key,
            request_hash,
            status,
            locked_until,
            expires_at
        )
        VALUES (
            @business_id,
            NULL,
            @operation_type,
            @idempotency_key,
            @request_hash,
            'IN_PROGRESS',
            transaction_timestamp() + interval '30 seconds',
            NULL
        )
        ON CONFLICT (business_id, operation_type, idempotency_key)
        DO NOTHING
        RETURNING id, request_hash, status::text, locked_until;
        """;

    private const string ReadExistingSql = """
        SELECT id, request_hash, status::text, locked_until,
               transaction_timestamp() AS database_now
        FROM idempotency_keys
        WHERE business_id = @business_id
          AND operation_type = @operation_type
          AND idempotency_key = @idempotency_key;
        """;

    private readonly NpgsqlDataSource _dataSource;

    internal ConfirmSaleIdempotencyStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    internal async Task<ConfirmSaleIdempotencyPhaseAResult> ResolveOrReserveAsync(
        long businessId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(businessId);
        ArgumentException.ThrowIfNullOrEmpty(idempotencyKey);
        ArgumentException.ThrowIfNullOrEmpty(requestHash);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        // A separate READ COMMITTED statement can see the competing insert after its commit.
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        ConfirmSaleIdempotencyPhaseAResult? result = null;

        await using (var command = new NpgsqlCommand(ReserveSql, connection, transaction))
        {
            AddScopeParameters(command, businessId, idempotencyKey);
            command.Parameters.AddWithValue("request_hash", NpgsqlDbType.Text, requestHash);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                // NewKey identifies a successful reservation; commit happens before returning.
                result = new ConfirmSaleIdempotencyPhaseAResult(
                    reader.GetInt64(0),
                    ConfirmSaleIdempotencyDecision.NewKey,
                    ReadSnapshot(reader));
            }
        }

        if (result is null)
        {
            await using var command = new NpgsqlCommand(ReadExistingSql, connection, transaction);
            AddScopeParameters(command, businessId, idempotencyKey);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                // Concurrent deletion after the conflict is not an acquired reservation.
                throw new InvalidOperationException(
                    "The conflicting confirm sale idempotency key could not be read.");
            }

            var snapshot = ReadSnapshot(reader);
            var databaseNow = reader.GetFieldValue<DateTimeOffset>(4);
            // Classification only: this read neither claims recovery nor renews the lease.
            result = new ConfirmSaleIdempotencyPhaseAResult(
                reader.GetInt64(0),
                ConfirmSaleIdempotencyResolver.Resolve(snapshot, requestHash, databaseNow),
                snapshot);
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static void AddScopeParameters(
        NpgsqlCommand command,
        long businessId,
        string idempotencyKey)
    {
        command.Parameters.AddWithValue("business_id", NpgsqlDbType.Bigint, businessId);
        command.Parameters.AddWithValue("operation_type", NpgsqlDbType.Text, OperationType);
        command.Parameters.AddWithValue("idempotency_key", NpgsqlDbType.Text, idempotencyKey);
    }

    private static ConfirmSaleIdempotencySnapshot ReadSnapshot(NpgsqlDataReader reader)
    {
        var status = reader.GetString(2) switch
        {
            "IN_PROGRESS" => ConfirmSaleIdempotencyStatus.InProgress,
            "COMPLETED" => ConfirmSaleIdempotencyStatus.Completed,
            "FAILED" => ConfirmSaleIdempotencyStatus.Failed,
            _ => throw new InvalidOperationException(
                "The persisted confirm sale idempotency key contains an unsupported status.")
        };

        var lockedUntil = reader.IsDBNull(3)
            ? (DateTimeOffset?)null
            : reader.GetFieldValue<DateTimeOffset>(3);

        return new ConfirmSaleIdempotencySnapshot(reader.GetString(1), status, lockedUntil);
    }
}
