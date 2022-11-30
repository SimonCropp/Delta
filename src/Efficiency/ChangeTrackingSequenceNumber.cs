using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Efficiency;

public static class ChangeTrackingSequenceNumber
{
    public static async Task EnableTracking(this DbConnection connection)
    {
        if (await IsTrackingEnabled(connection))
        {
            return;
        }

        await using var trackChangesOnDbCommand = connection.CreateCommand();
        trackChangesOnDbCommand.CommandText = $@"
alter database {connection.Database}
set change_tracking = on
(
    change_retention = 30 days,
    auto_cleanup = on
)";
        await trackChangesOnDbCommand.ExecuteNonQueryAsync();
    }

    public static async Task<bool> IsTrackingEnabled(this DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select count(d.name)
from sys.databases as d inner join
     sys.change_tracking_databases as t on
     t.database_id = d.database_id
where d.name = @name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "name";
        parameter.DbType = DbType.String;
        parameter.Value = connection.Database;
        command.Parameters.Add(parameter);
        var x  = (int)(await command.ExecuteScalarAsync())!;
        return x == 1;
    }

    public static async Task DisableTracking(this DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"alter database [{connection.Database}] set change_tracking = off;";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<IReadOnlyList<string>> GetDatabasesWithTracking(this DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
select d.name
from sys.databases as d inner join
     sys.change_tracking_databases as t on
     t.database_id = d.database_id";
        await using var reader = await command.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync().ConfigureAwait(false))
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