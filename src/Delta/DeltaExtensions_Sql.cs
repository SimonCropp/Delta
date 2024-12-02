// ReSharper disable UseRawString

namespace Delta;

public static partial class DeltaExtensions
{
    internal static async Task<string> GetLastTimeStamp(SqlConnection connection, SqlTransaction? transaction = null, Cancel cancel = default)
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

    public static async Task<string> GetLastTimeStamp(this SqlConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        return await ExecuteTimestampQuery(command, cancel);
    }

    static async Task<string> ExecuteTimestampQuery(SqlCommand command, Cancel cancel = default)
    {
        command.CommandText = @"
-- begin-snippet: SqlTimestamp
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
}