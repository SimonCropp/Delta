using Microsoft.Net.Http.Headers;
// ReSharper disable UseRawString

namespace Delta;

public static partial class DeltaExtensions
{
    public static void NoStore(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "no-store, max-age=0");

    public static void NoCache(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "no-cache");

    public static void CacheForever(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "public, max-age=31536000, immutable");

    static bool IsImmutableCache(this HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header.Contains("immutable"))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task SetTrackedTables(
        this SqlConnection connection,
        IEnumerable<string> tablesToTrack,
        uint retentionDays = 1,
        Cancel cancel = default)
    {
        await connection.EnableTracking(retentionDays, cancel);

        var trackedTables = await connection.GetTrackedTables(cancel);

        tablesToTrack = tablesToTrack.ToList();

        var builder = new StringBuilder();
        var except = tablesToTrack.Except(trackedTables, StringComparer.OrdinalIgnoreCase);
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
        await command.ExecuteNonQueryAsync(cancel);
    }

    public static async Task EnableTracking(
        this SqlConnection connection,
        uint retentionDays = 1,
        Cancel cancel = default)
    {
        if (await IsTrackingEnabled(connection, cancel))
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
        await command.ExecuteNonQueryAsync(cancel);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedTables(this SqlConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
-- begin-snippet: GetTrackedTablesSql
select t.Name
from sys.tables as t left join
  sys.change_tracking_tables as c on t.[object_id] = c.[object_id]
where c.[object_id] is not null
-- end-snippet";
        await using var reader = await command.ExecuteReaderAsync(cancel);
        var list = new List<string>();
        while (await reader.ReadAsync(cancel))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }

    public static async Task<bool> IsTrackingEnabled(this SqlConnection connection, Cancel cancel = default)
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
        return await command.ExecuteScalarAsync(cancel) is 1;
    }

    public static async Task DisableTracking(this SqlConnection connection, Cancel cancel = default)
    {
        if (!await IsTrackingEnabled(connection, cancel))
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var table in await connection.GetTrackedTables(cancel))
        {
            builder.AppendLine($@"
-- begin-snippet: DisableTrackingSqlTable
alter table [{table}] disable change_tracking;
-- end-snippet
");
        }

        builder.AppendLine($@"
-- begin-snippet: DisableTrackingSqlDB
alter database [{connection.Database}] set change_tracking = off;
-- end-snippet");
        await using var command = connection.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync(cancel);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedDatabases(
        this SqlConnection connection,
        Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
-- begin-snippet: GetTrackedDatabasesSql
select d.name
from sys.databases as d inner join
  sys.change_tracking_databases as t on
  t.database_id = d.database_id
-- end-snippet";
        await using var reader = await command.ExecuteReaderAsync(cancel);
        var list = new List<string>();
        while (await reader.ReadAsync(cancel))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }

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
    internal static string AssemblyWriteTime;

    static DeltaExtensions()
    {
        #region AssemblyWriteTime

        var webAssemblyLocation = Assembly.GetEntryAssembly()!.Location;
        AssemblyWriteTime = File.GetLastWriteTime(webAssemblyLocation).Ticks.ToString();

        #endregion
    }

    internal static async Task<bool> HandleRequest(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, Task<string>> getTimeStamp, Func<HttpContext, bool>? shouldExecute, LogLevel level)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Path;

        if (request.Method != "GET")
        {
            logger.Log(level, "Delta {path}: Skipping since request is {method}", path, request.Method);
            return false;
        }

        if (response.Headers.ETag.Count != 0)
        {
            logger.Log(level, "Delta {path}: Skipping since response has an ETag", path);
            return false;
        }

        if (response.IsImmutableCache())
        {
            logger.Log(level, "Delta {path}: Skipping since response has CacheControl=immutable", path);
            return false;
        }

        if (shouldExecute != null && !shouldExecute(context))
        {
            logger.Log(level, "Delta {path}: Skipping since shouldExecute is false", path);
            return false;
        }

        var timeStamp = await getTimeStamp(context);
        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(timeStamp, suffixValue);
        response.Headers.ETag = etag;
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            logger.Log(level, "Delta {path}: Skipping since request has no If-None-Match", path);
            return false;
        }

        if (ifNoneMatch != etag)
        {
            logger.Log(
                level,
                """
                Delta {path}: Skipping since If-None-Match != ETag
                If-None-Match: {ifNoneMatch}
                ETag: {etag}
                """,
                path,
                ifNoneMatch,
                etag);
            return false;
        }

        logger.Log(level, "Delta {path}: 304", path);
        response.StatusCode = 304;
        response.NoCache();
        return true;
    }

    static ILogger GetLogger(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger("Delta");
    }

    #region BuildEtag

    internal static string BuildEtag(string timeStamp, string? suffix)
    {
        if (suffix == null)
        {
            return $"\"{AssemblyWriteTime}-{timeStamp}\"";
        }

        return $"\"{AssemblyWriteTime}-{timeStamp}-{suffix}\"";
    }

    #endregion
}