namespace Delta;

public  static partial class Delta
{
    public static async Task SetTrackedTables(this DbConnection connection, IEnumerable<string> tablesToTrack, uint retentionDays = 1,CancellationToken cancellation = default)
    {
        await connection.EnableTracking(retentionDays, cancellation: cancellation);

        var trackedTables = await connection.GetTrackedTables(cancellation: cancellation);

        tablesToTrack = tablesToTrack.ToList();

        var builder = new StringBuilder();
        var except = tablesToTrack.Except(trackedTables, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var table in except)
        {
            builder.AppendLine($"alter table [{table}] enable change_tracking;");
        }

        var tablesToDisable = trackedTables.Except(tablesToTrack);
        foreach (var table in tablesToDisable)
        {
            builder.AppendLine($"alter table [{table}] disable change_tracking;");
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
alter database {connection.Database}
set change_tracking = on
(
    change_retention = {retentionDays} days,
    auto_cleanup = on
)";
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedTables(this DbConnection connection, CancellationToken cancellation = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select t.Name
from sys.tables as t left join
    sys.change_tracking_tables as c on t.[object_id] = c.[object_id]
where c.[object_id] is not null";
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
select count(d.name)
from sys.databases as d inner join
    sys.change_tracking_databases as t on
    t.database_id = d.database_id
where d.name = '{connection.Database}'";
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
            builder.AppendLine($"alter table [{table}] disable change_tracking;");
        }

        builder.AppendLine($"alter database [{connection.Database}] set change_tracking = off;");
        await using var command = connection.CreateCommand();
        command.CommandText = builder.ToString();
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<IReadOnlyList<string>> GetTrackedDatabases(this DbConnection connection, CancellationToken cancellation = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select d.name
from sys.databases as d inner join
    sys.change_tracking_databases as t on
    t.database_id = d.database_id";
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var list = new List<string>();
        while (await reader.ReadAsync(cancellation))
        {
            list.Add((string) reader[0]);
        }

        return list;
    }

    public static void AddRowVersionProperty<T>(this EntityTypeBuilder<T> builder)
        where T : class, IRowVersion =>
        builder.Property(nameof(IRowVersion.RowVersion))
            .IsBytesRowVersion();

    public static void IsBytesRowVersion(this PropertyBuilder property) =>
        property
            .IsRowVersion()
            .HasConversion<byte[]>();

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
declare @changeTracking bigint = change_tracking_current_version();
declare @timeStamp bigint = convert(bigint, @@dbts);

if (@changeTracking is null)
    select cast(@timeStamp as varchar) 
else
    select cast(@timeStamp as varchar) + '-' + cast(@changeTracking as varchar) 
";
        return (string) (await command.ExecuteScalarAsync(token))!;
    }
}