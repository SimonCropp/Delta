using Microsoft.Net.Http.Headers;

namespace Delta;

public static partial class DeltaExtensions
{
    public static void NoStore(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "no-store, max-age=0");

    public static void CacheForever(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "public, max-age=31536000, immutable");

    internal static bool IsImmutableCache(this HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(_ => _ == "immutable"))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task SetTrackedTables(this DbConnection connection, IEnumerable<string> tablesToTrack, uint retentionDays = 1,CancellationToken cancellation = default)
    {
        await connection.EnableTracking(retentionDays, cancellation: cancellation);

        var trackedTables = await connection.GetTrackedTables(cancellation: cancellation);

        tablesToTrack = tablesToTrack.ToList();

        var builder = new StringBuilder();
        var except = tablesToTrack.Except(trackedTables, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var table in except)
        {
            builder.AppendLine($@"
-- begin-snippet: EnableTrackingTableSql
alter table [{table}] enable change_tracking
-- end-snippet");
        }

        var tablesToDisable = trackedTables.Except(tablesToTrack);
        foreach (var table in tablesToDisable)
        {
            builder.AppendLine($@"
-- begin-snippet: DisableTrackingTableSql
alter table [{table}] disable change_tracking;
-- end-snippet");
        }

        if (builder.Length == 0)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task EnableTracking(this DbConnection connection, uint retentionDays = 1, CancellationToken cancellation = default)
    {
        if (await IsTrackingEnabled(connection, cancellation))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
-- begin-snippet: EnableTrackingSql
alter database {connection.Database}
set change_tracking = on
(
    change_retention = {retentionDays} days,
    auto_cleanup = on
)
-- end-snippet";
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedTables(this DbConnection connection, CancellationToken cancellation = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
-- begin-snippet: GetTrackedTablesSql
select t.Name
from sys.tables as t left join
    sys.change_tracking_tables as c on t.[object_id] = c.[object_id]
where c.[object_id] is not null
-- end-snippet";
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var list = new List<string>();
        while (await reader.ReadAsync(cancellation))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }

    public static async Task<bool> IsTrackingEnabled(this DbConnection connection, CancellationToken cancellation = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
-- begin-snippet: IsTrackingEnabledSql
select count(d.name)
from sys.databases as d inner join
    sys.change_tracking_databases as t on
    t.database_id = d.database_id
where d.name = '{connection.Database}'
-- end-snippet";
        return await command.ExecuteScalarAsync(cancellation) is 1;
    }

    public static async Task DisableTracking(this DbConnection connection, CancellationToken cancellation = default)
    {
        if (!await IsTrackingEnabled(connection, cancellation))
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var table in await connection.GetTrackedTables(cancellation: cancellation))
        {
            builder.AppendLine($@"
-- begin-snippet: DisableTrackingSql
alter table [{table}] disable change_tracking;
-- end-snippet
");
        }

        builder.AppendLine($@"
-- begin-snippet: DisableTrackingSql
alter database [{connection.Database}] set change_tracking = off;
-- end-snippet");
        await using var command = connection.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedDatabases(this DbConnection connection, CancellationToken cancellation = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
-- begin-snippet: GetTrackedDatabasesSql
select d.name
from sys.databases as d inner join
    sys.change_tracking_databases as t on
    t.database_id = d.database_id
-- end-snippet";
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var list = new List<string>();
        while (await reader.ReadAsync(cancellation))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }

    public static async Task<string> GetLastTimeStamp(this DbContext context, CancellationToken token = default)
    {
        // Do not dispose of this connection as it kill the context
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        var transaction = context.Database.CurrentTransaction;
        if (transaction != null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        if (connection.State != ConnectionState.Closed)
        {
            return await ExecuteTimestampQuery(command, token);
        }

        await connection.OpenAsync(token);
        try
        {
            return await ExecuteTimestampQuery(command, token);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<string> GetLastTimeStamp(this DbConnection connection, CancellationToken token = default)
    {
        await using var command = connection.CreateCommand();
        return await ExecuteTimestampQuery(command, token);
    }

    static async Task<string> ExecuteTimestampQuery(DbCommand command, CancellationToken token = default)
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
        return (string) (await command.ExecuteScalarAsync(token))!;
    }
}