// ReSharper disable UseRawString

namespace Delta;
class NpgsqlTimestampQueryExecutor : ITimestampQueryExecutor
{
    public async Task<string> Execute(DbCommand command, Cancel cancel)
    {
        command.CommandText = @"
        -- begin-snippet: PostgresTimestamp
        select pg_last_committed_xact();
        -- end-snippet
        ";
        var result = (object[]?)await command.ExecuteScalarAsync(cancel);
        // null on first run after SET track_commit_timestamp to 'on'
        if (result is null)
        {
            return string.Empty;
        }

        var xid = (uint)result[0];
        return xid.ToString();
    }
}