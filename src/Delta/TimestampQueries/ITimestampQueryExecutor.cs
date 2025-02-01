namespace Delta;

interface ITimestampQueryExecutor
{
    Task<string> Execute(DbCommand command, Cancel cancel);
}