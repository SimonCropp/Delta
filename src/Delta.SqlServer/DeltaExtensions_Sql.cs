namespace Delta;

public static class TrackingExtensions
{
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
            builder.AppendLine(
                $"""
                 -- begin-snippet: EnableTrackingTableSql
                 alter table [{table}] enable change_tracking
                 -- end-snippet
                 """);
        }

        var tablesToDisable = trackedTables.Except(tablesToTrack);
        foreach (var table in tablesToDisable)
        {
            builder.AppendLine(
                $"""
                 -- begin-snippet: DisableTrackingTableSql
                 alter table [{table}] disable change_tracking;
                 -- end-snippet
                 """);
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
        var database = connection.Database;
        command.CommandText =
            $"""
             -- begin-snippet: EnableTrackingSql
             alter database {database}
             set change_tracking = on
             (
               change_retention = {retentionDays} days,
               auto_cleanup = on
             )
             -- end-snippet
             """;
        await command.ExecuteNonQueryAsync(cancel);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedTables(this SqlConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            -- begin-snippet: GetTrackedTablesSql
            select t.Name
            from sys.tables as t inner join
              sys.change_tracking_tables as c on t.[object_id] = c.[object_id]
            -- end-snippet
            """;
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
        command.CommandText =
            """
             -- begin-snippet: IsTrackingEnabledSql
             select count(d.name)
             from sys.databases as d inner join
               sys.change_tracking_databases as t on
               t.database_id = d.database_id
             where d.name = @database
             -- end-snippet
             """;
        command.Parameters.AddWithValue("@database", connection.Database);
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
            builder.AppendLine(
                $"""
                 -- begin-snippet: DisableTrackingSqlTable
                 alter table [{table}] disable change_tracking;
                 -- end-snippet
                 """);
        }

        var database = connection.Database;
        builder.AppendLine(
            $"""
             -- begin-snippet: DisableTrackingSqlDB
             alter database [{database}] set change_tracking = off;
             -- end-snippet
             """);
        await using var command = connection.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync(cancel);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedDatabases(
        this SqlConnection connection,
        Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            -- begin-snippet: GetTrackedDatabasesSql
            select d.name
            from sys.databases as d inner join
              sys.change_tracking_databases as t on
              t.database_id = d.database_id
            -- end-snippet
            """;
        await using var reader = await command.ExecuteReaderAsync(cancel);
        var list = new List<string>();
        while (await reader.ReadAsync(cancel))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }
}