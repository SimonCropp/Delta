namespace Delta;

public static partial class DeltaExtensions
{
    internal static Task<string> GetLastTimeStamp(DbConnection connection, DbTransaction? transaction = null, Cancel cancel = default) =>
        ExecuteCommand(connection, transaction, ExecuteTimestampQuery, cancel);

    static async Task<string> ExecuteCommand(DbConnection connection, DbTransaction? transaction, Func<DbCommand, Cancel, Task<string>> execute, Cancel cancel)
    {
        await using var command = connection.CreateCommand();
        if (transaction != null)
        {
            command.Transaction = transaction;
        }

        if (connection.State != ConnectionState.Closed)
        {
            return await execute(command, cancel);
        }

        await connection.OpenAsync(cancel);
        try
        {
            return await execute(command, cancel);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<string> GetLastTimeStamp(this DbConnection connection, Cancel cancel = default)
    {
        await using var command = connection.CreateCommand();
        return await ExecuteTimestampQuery(command, cancel);
    }

    static async Task<string> ExecuteTimestampQuery(DbCommand command, Cancel cancel = default)
    {
        var timestampQueryExecutor = TimestampQueryExecutorFactory.Create(command);
        return await timestampQueryExecutor.Execute(command, cancel);
    }
}