namespace Delta;

public static partial class DeltaExtensions
{
    public static async Task<string> GetLastTimeStamp(this DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        if (transaction != null)
        {
            command.Transaction = transaction;
        }

        if (connection.State != ConnectionState.Closed)
        {
            return await ExecuteTimestampQuery(command, cancel);
        }

        await connection.OpenAsync(cancel);
        try
        {
            return await ExecuteTimestampQuery(command, cancel);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    static async Task<string> ExecuteTimestampQuery(DbCommand command, Cancel cancel = default)
    {
        var name = command.GetType().Name;
        if (name == "SqlCommand")
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

        if (name == "NpgsqlCommand")
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

        throw new("Unsupported type " + name);
    }
}