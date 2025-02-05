// ReSharper disable UseRawString

namespace Delta;

public static partial class DeltaExtensions
{
    internal static Task<string> GetLastTimeStamp(DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default) =>
        ExecuteCommand(connection, transaction, ExecuteTimestampQuery, cancel);

    static async Task<string> ExecuteCommand(DbConnection connection, DbTransaction? transaction, Func<DbCommand, Cancel, Task<string>> execute, Cancel cancel)
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

    public static async Task<string> GetLastTimeStamp(this DbConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        return await ExecuteTimestampQuery(command, cancel);
    }

    static string? lsn;

    internal static void ClearLsn() => lsn = null;

    static async Task<string> ExecuteTimestampQuery(DbCommand command, Cancel cancel = default)
    {
        var name = command.GetType().Name;
        if (name == "SqlCommand")
        {
            var lsnString = lsn is null ? "null" : $"'0x{lsn}'";
            command.CommandText = $@"
-- begin-snippet: SqlServerTimestamp
declare @changeTracking bigint = change_tracking_current_version();
declare @timeStamp bigint = convert(bigint, @@dbts);

declare @timeResult varchar;
if (@changeTracking is null)
  set @timeResult = cast(@timeStamp as varchar)
else
  set @timeResult = cast(@timeStamp as varchar) + '-' + cast(@changeTracking as varchar)

select top 1 [End Time] + '-' + @timeResult, [Current LSN]
from fn_dblog({lsnString}, null)
where Operation = 'LOP_COMMIT_XACT'
order by [End Time] desc;
-- end-snippet
";

            var startNew = Stopwatch.StartNew();
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancel);
            var readAsync = await reader.ReadAsync(cancel);
            // for empty transaction log
            if(!readAsync)
            {
                return string.Empty;
            }

            var endTime = (string)reader[0];
            lsn = (string)reader[1];
            Debug.WriteLine(startNew.ElapsedMilliseconds);
            return endTime;
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