// ReSharper disable UseRawString

namespace Delta;

public static partial class DeltaExtensions
{
    internal static async Task<string> GetLastTimeStamp(DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default)
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

    public static async Task<string> GetLastTimeStamp(this DbConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        return await ExecuteTimestampQuery(command, cancel);
    }

    static async Task<string> ExecuteTimestampQuery(DbCommand command, Cancel cancel = default)
    {
        var name = command.GetType().Name;
        if (name == "SqlCommand")
        {
            command.CommandText = @"
-- begin-snippet: SqlServerTimestamp
declare @changeTracking bigint = change_tracking_current_version();
declare @timeStamp bigint = convert(bigint, @@dbts);

if (@changeTracking is null)
  select cast(@timeStamp as varchar)
else
  select cast(@timeStamp as varchar) + '-' + cast(@changeTracking as varchar)
-- end-snippet
";
            return (string) (await command.ExecuteScalarAsync(cancel))!;
        }

        if (name == "NpgsqlCommand")
        {
            command.CommandText = @"
-- begin-snippet: PostgresTimestamp
select pg_last_committed_xact();
-- end-snippet
";
            var result = (object[]?) await command.ExecuteScalarAsync(cancel);
            // null on first run after SET track_commit_timestamp to 'on'
            if (result is null)
            {
                return string.Empty;
            }

            var xid = (uint) result[0];
            return xid.ToString();
        }

        throw new("Unsupported type " + name);
    }
}