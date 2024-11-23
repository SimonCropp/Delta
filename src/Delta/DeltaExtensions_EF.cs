namespace Delta;

public static partial class DeltaExtensions
{
    public static Task<string> GetLastTimeStamp(this DbContext context, Cancel cancel = default)
    {
        // Do not dispose of this connection as it kills the context
        var database = context.Database;
        var connection = database.GetDbConnection();
        var transaction = database.CurrentTransaction?.GetDbTransaction();
        return GetLastTimeStamp(connection, transaction, cancel);
    }
}