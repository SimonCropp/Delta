using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Efficiency;

public static class ChangeTrackingSequenceNumber
{
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
  select cast(@changeTracking as varchar) + '_'+ cast(@timeStamp as varchar) 
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

    static async Task<T> ExecuteScalarOnCurrentConnection<T>(this DbContext context, string commandText, CancellationToken token = default)
    {
        // Do not dispose of this connection as it kill the context
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        var transaction = context.Database.CurrentTransaction;
        if (transaction != null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        command.CommandText = commandText;

        if (connection.State != ConnectionState.Closed)
        {
            return (T) (await command.ExecuteScalarAsync(token))!;
        }

        await connection.OpenAsync(token);
        try
        {
            return (T) (await command.ExecuteScalarAsync(token))!;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}