namespace Delta;

static class TimestampQueryExecutorFactory
{
    public static ITimestampQueryExecutor Create(DbCommand command) =>
        command.GetType().Name switch
        {
            "SqlCommand" => new SqlServerTimestampQueryExecutor(),
            "NpgsqlCommand" => new NpgsqlTimestampQueryExecutor(),
            _ => throw new NotSupportedException("Unsupported type " + command.GetType().Name)
        };
}