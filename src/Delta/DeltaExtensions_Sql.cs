namespace Delta;

public static partial class DeltaExtensions
{
    public static async Task<string> GetLastTimeStamp(this DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default)
    {
        query ??= await ResolveQuery(connection, transaction, cancel);

        return await Execute(connection, transaction, query, cancel);
    }

    static async Task<Func<DbCommand, Cancel, Task<string>>> ResolveQuery(DbConnection connection, DbTransaction? transaction, Cancel cancel)
    {
        var name = connection.GetType().Name;

        if (name == "SqlConnection")
        {
            if (await Execute(connection, transaction, HasViewServerState, cancel))
            {
                return ExecuteSqlLsn;
            }

            if (await Execute(connection, transaction, HasChangeTracking, cancel))
            {
                return ExecuteSqlTimeStampWithChangeTracking;
            }

            return ExecuteSqlTimeStamp;
        }

        if (name == "NpgsqlConnection")
        {
            return ExecutePostgres;
        }

        throw new($"Unsupported type {name}");
    }

    static async Task<T> Execute<T>(DbConnection connection, DbTransaction? transaction, Func<DbCommand, Cancel, Task<T>> execute, Cancel cancel)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        if (connection.State != ConnectionState.Closed)
        {
            return await execute(command, cancel);
        }

        await connection.OpenAsync(cancel);
        try
        {
            return await execute(command, cancel);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    static async Task<string> ExecutePostgres(DbCommand command, Cancel cancel = default)
    {
        command.CommandText = """
                              --begin-snippet: PostgresTimeStamp
                              select pg_last_committed_xact();
                              --end-snippet
                              """;
        var results = (object?[]?) await command.ExecuteScalarAsync(cancel);

        // null on first run after SET track_commit_timestamp to 'on'
        var result = results?[0];
        if (result is null)
        {
            return string.Empty;
        }

        var xid = (uint) result;
        return xid.ToString();
    }

    internal static async Task<string> ExecuteSqlLsn(DbCommand command, Cancel cancel = default)
    {
        #region SqlServerTimeStampWithServerState

        const string logRndLsnCommand =
            """
            select log_end_lsn
            from sys.dm_db_log_stats(db_id())
            """;

        #endregion

        command.CommandText = logRndLsnCommand;

        try
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancel);
            var readAsync = await reader.ReadAsync(cancel);
            // for empty transaction log
            if (!readAsync)
            {
                return string.Empty;
            }

            return (string) reader[0];
        }
        catch (DbException exception)
        {
            throw new(
                $"""
                 Failed to execute log_end_lsn:
                 {logRndLsnCommand}
                 It is possible the current user does not have the VIEW SERVER STATE permission.
                 """,
                exception);
        }
    }

    internal static async Task<string> ExecuteSqlTimeStamp(DbCommand command, Cancel cancel = default)
    {
        command.CommandText =
            """
            --begin-snippet: SqlServerTimeStamp
            declare @timeStamp bigint = convert(bigint, @@dbts);
            select cast(@timeStamp as varchar)
            --end-snippet
            """;
        return (string) (await command.ExecuteScalarAsync(cancel))!;
    }

    internal static async Task<string> ExecuteSqlTimeStampWithChangeTracking(DbCommand command, Cancel cancel = default)
    {
        command.CommandText =
            """
            --begin-snippet: SqlServerTimeStampWithChangeTracking
            declare @changeTracking bigint = change_tracking_current_version();
            declare @timeStamp bigint = convert(bigint, @@dbts);
            select cast(@timeStamp as varchar) + '-' + cast(@changeTracking as varchar)
            --end-snippet
            """;
        return (string) (await command.ExecuteScalarAsync(cancel))!;
    }

    static async Task<bool> HasViewServerState(DbCommand command, Cancel cancel = default)
    {
        command.CommandText = "select has_perms_by_name(null, null, 'VIEW SERVER STATE')";
        var result = (int)(await command.ExecuteScalarAsync(cancel))!;
        return result == 1;
    }

    static async Task<bool> HasChangeTracking(DbCommand command, Cancel cancel = default)
    {
        command.CommandText = "change_tracking_current_version()";
        var result = await command.ExecuteScalarAsync(cancel);
        return result != null;
    }

    static Func<DbCommand, Cancel, Task<string>>? query;

    internal static void Reset() => query = null;
}