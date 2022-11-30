using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Efficiency;

public static class ChangeTrackingSequenceNumber
{
    public static async Task EnableTracking(this DbConnection connection, CancellationToken cancellation = default)
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
    change_retention = 30 days,
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
        var stringBuilder = new StringBuilder();
        foreach (var table in await connection.GetTrackedTables(cancellation: cancellation))
        {
            stringBuilder.AppendLine($"alter table [{table}] disable change_tracking;");
        }

        stringBuilder.AppendLine($"alter database [{connection.Database}] set change_tracking = off;");
        await using var command = connection.CreateCommand();
        command.CommandText = stringBuilder.ToString();
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public static async Task<IReadOnlyList<string>> GetDatabasesWithTracking(this DbConnection connection, CancellationToken cancellation = default)
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
            .IsRowVersion()
            .HasConversion<byte[]>();

    public static async Task<string> LastTimeStamp(this DbContext context, CancellationToken token = default)
    {
        // Do not dispose of this connection as it kill the context
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        var transaction = context.Database.CurrentTransaction;
        if (transaction != null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        command.CommandText = @"
declare @changeTracking bigint = change_tracking_current_version();
declare @timeStamp bigint = convert(bigint, @@dbts);

if (@changeTracking is null)
    select cast(@timeStamp as varchar) 
else
    select cast(@changeTracking as varchar) + '_' + cast(@timeStamp as varchar) 
";

        if (connection.State != ConnectionState.Closed)
        {
            return (string) (await command.ExecuteScalarAsync(token))!;
        }

        await connection.OpenAsync(token);
        try
        {
            return (string) (await command.ExecuteScalarAsync(token))!;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}