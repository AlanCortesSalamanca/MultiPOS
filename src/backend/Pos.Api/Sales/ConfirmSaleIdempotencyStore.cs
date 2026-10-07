using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Pos.Api.Sales;

internal sealed record ConfirmSaleIdempotencyPhaseAResult(
    long Id,
    ConfirmSaleIdempotencyDecision Decision,
    ConfirmSaleIdempotencySnapshot Snapshot);

internal enum ConfirmSaleIdempotencyExecutionLockDecision
{
    // Exclusive continuation toward branch/barrier/history; not permission to create a sale.
    ContinuationOwned,
    KeyReused,
    InProgress,
    CompletedReplay,
    FailedReplay,
    ReconciliationRequired
}

internal sealed record ConfirmSaleIdempotencyExecutionLockResult(
    long Id,
    ConfirmSaleIdempotencyExecutionLockDecision Decision,
    ConfirmSaleIdempotencySnapshot? Snapshot,
    long? ResultEntityId,
    bool RowLockHeld);

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

    private const string LockForContinuationSql = """
        SELECT id, request_hash, status::text, locked_until, result_entity_id,
               transaction_timestamp() AS database_now
        FROM idempotency_keys
        WHERE id = @id
          AND business_id = @business_id
          AND operation_type = @operation_type
          AND idempotency_key = @idempotency_key
        FOR UPDATE NOWAIT;
        """;

    private const string RenewRecoveryLeaseSql = """
        UPDATE idempotency_keys
        SET locked_until = transaction_timestamp() + interval '30 seconds'
        WHERE id = @id
          AND business_id = @business_id
          AND operation_type = @operation_type;
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

    // Call immediately after BEGIN: idempotency must be the first persistent lock.
    // The caller owns the connection/transaction and must retain this transaction for later phases.
    internal async Task<ConfirmSaleIdempotencyExecutionLockResult> LockForContinuationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long businessId,
        string idempotencyKey,
        string requestHash,
        ConfirmSaleIdempotencyPhaseAResult phaseAResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(phaseAResult);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(businessId);
        ArgumentException.ThrowIfNullOrEmpty(idempotencyKey);
        ArgumentException.ThrowIfNullOrEmpty(requestHash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(phaseAResult.Id);
        ValidateContinuationDecision(phaseAResult.Decision);

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

        try
        {
            await using var command = new NpgsqlCommand(LockForContinuationSql, connection, transaction);
            AddScopeParameters(command, businessId, idempotencyKey);
            command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, phaseAResult.Id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "The reserved confirm sale idempotency key could not be locked and read.");
            }

            var snapshot = ReadSnapshot(reader);
            var resultEntityId = reader.IsDBNull(4) ? (long?)null : reader.GetInt64(4);
            var databaseNow = reader.GetFieldValue<DateTimeOffset>(5);
            var decision = ClassifyLockedRow(
                phaseAResult.Decision,
                requestHash,
                snapshot,
                resultEntityId,
                databaseNow);

            // Disposing the command/reader does not release the caller's transaction row lock.
            return new ConfirmSaleIdempotencyExecutionLockResult(
                reader.GetInt64(0), decision, snapshot, resultEntityId, true);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            // NOWAIT failure aborts the PostgreSQL transaction; the caller must end it, not continue.
            // No authoritative snapshot was acquired under lock.
            return new ConfirmSaleIdempotencyExecutionLockResult(
                phaseAResult.Id, ConfirmSaleIdempotencyExecutionLockDecision.InProgress, null, null, false);
        }
    }

    // Call immediately after eligible A2, on the same caller-owned transaction retaining its row lock.
    internal async Task RenewRecoveryLeaseAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long businessId,
        ConfirmSaleIdempotencyPhaseAResult phaseAResult,
        ConfirmSaleIdempotencyExecutionLockResult executionLockResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(businessId);
        ValidateRecoveryLeaseRenewal(phaseAResult, executionLockResult);

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

        await using var command = new NpgsqlCommand(RenewRecoveryLeaseSql, connection, transaction);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, executionLockResult.Id);
        command.Parameters.AddWithValue("business_id", NpgsqlDbType.Bigint, businessId);
        command.Parameters.AddWithValue("operation_type", NpgsqlDbType.Text, OperationType);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                "Confirm sale recovery lease renewal must affect exactly one idempotency key.");
        }
    }

    internal static void ValidateRecoveryLeaseRenewal(
        ConfirmSaleIdempotencyPhaseAResult phaseAResult,
        ConfirmSaleIdempotencyExecutionLockResult executionLockResult)
    {
        ArgumentNullException.ThrowIfNull(phaseAResult);
        ArgumentNullException.ThrowIfNull(executionLockResult);

        if (phaseAResult.Decision != ConfirmSaleIdempotencyDecision.RecoveryCheckRequired ||
            executionLockResult.Decision != ConfirmSaleIdempotencyExecutionLockDecision.ContinuationOwned ||
            !executionLockResult.RowLockHeld)
        {
            throw new InvalidOperationException(
                "Confirm sale recovery lease renewal requires RecoveryCheckRequired, ContinuationOwned and a held row lock.");
        }

        if (phaseAResult.Id <= 0 || executionLockResult.Id != phaseAResult.Id)
        {
            throw new InvalidOperationException(
                "Confirm sale recovery lease renewal requires the same reserved and locked idempotency key.");
        }
    }

    internal static ConfirmSaleIdempotencyExecutionLockDecision ClassifyLockedRow(
        ConfirmSaleIdempotencyDecision phaseADecision,
        string currentRequestHash,
        ConfirmSaleIdempotencySnapshot snapshot,
        long? resultEntityId,
        DateTimeOffset databaseNow)
    {
        ValidateContinuationDecision(phaseADecision);
        ArgumentException.ThrowIfNullOrEmpty(currentRequestHash);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (string.IsNullOrEmpty(snapshot.RequestHash))
        {
            throw new InvalidOperationException(
                "The locked confirm sale idempotency snapshot requires a request hash.");
        }

        // Recheck the persisted hash before interpreting status, lease or result association.
        if (!string.Equals(snapshot.RequestHash, currentRequestHash, StringComparison.Ordinal))
        {
            return ConfirmSaleIdempotencyExecutionLockDecision.KeyReused;
        }

        switch (snapshot.Status)
        {
            case ConfirmSaleIdempotencyStatus.Completed:
                return ConfirmSaleIdempotencyExecutionLockDecision.CompletedReplay;

            case ConfirmSaleIdempotencyStatus.Failed:
                return ConfirmSaleIdempotencyExecutionLockDecision.FailedReplay;

            case ConfirmSaleIdempotencyStatus.InProgress:
                var lockedUntil = snapshot.LockedUntil ?? throw new InvalidOperationException(
                    "An in-progress locked confirm sale idempotency snapshot requires a lease.");

                if (phaseADecision == ConfirmSaleIdempotencyDecision.RecoveryCheckRequired &&
                    lockedUntil > databaseNow)
                {
                    return ConfirmSaleIdempotencyExecutionLockDecision.InProgress;
                }

                if (resultEntityId is not null)
                {
                    return ConfirmSaleIdempotencyExecutionLockDecision.ReconciliationRequired;
                }

                // NewKey may retain its own lease. Recovery candidates require a non-current lease.
                // Both paths still require branch/barrier/historical reconciliation before sale effects.
                return ConfirmSaleIdempotencyExecutionLockDecision.ContinuationOwned;

            default:
                throw new InvalidOperationException(
                    "The locked confirm sale idempotency snapshot contains an unsupported status.");
        }
    }

    private static void ValidateContinuationDecision(ConfirmSaleIdempotencyDecision decision)
    {
        if (decision is not (ConfirmSaleIdempotencyDecision.NewKey or
            ConfirmSaleIdempotencyDecision.RecoveryCheckRequired))
        {
            throw new ArgumentException(
                "Confirm sale execution locking requires NewKey or RecoveryCheckRequired from Phase A.",
                nameof(decision));
        }
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
