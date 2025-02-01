// ReSharper disable UseRawString

namespace Delta;
class SqlServerTimestampQueryExecutor : ITimestampQueryExecutor
{
    public async Task<string> Execute(DbCommand command, Cancel cancel)
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
        return (string)(await command.ExecuteScalarAsync(cancel))!;
    }
}