namespace Delta;

public static partial class DeltaExtensions
{
    public static async Task<string> GetLastTimeStamp(this DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default)
    {
        InitQuery(connection);

        return await Execute(connection, transaction, query, cancel);
    }

    [MemberNotNull(nameof(query))]
    static void InitQuery(DbConnection connection)
    {
        if (query != null)
        {
            return;
        }

        var name = connection.GetType().Name;
        if (name == "SqlConnection")
        {
            query = ExecuteSql;
            return;
        }

        if (name == "NpgsqlConnection")
        {
            query = ExecutePostgres;
            return;
        }

        throw new($"Unsupported type {name}");
    }

    static async Task<T> Execute<T>(DbConnection connection, DbTransaction? transaction, Func<DbCommand, Cancel, Task<T>> execute, Cancel cancel)
    {
        await using var command = connection.CreateCommand();
        if (transaction != null)
        {
            command.Transaction = transaction;
        }

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
        command.CommandText = "select pg_last_committed_xact();";
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

    static async Task<string> ExecuteSql(DbCommand command, Cancel cancel = default)
    {
        command.CommandText = $"select log_end_lsn from sys.dm_db_log_stats(db_id())";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancel);
        var readAsync = await reader.ReadAsync(cancel);
        // for empty transaction log
        if (!readAsync)
        {
            return string.Empty;
        }

        return (string) reader[0];
    }

    static Func<DbCommand, Cancel, Task<string>>? query;

    internal static void Reset() => query = null;
}