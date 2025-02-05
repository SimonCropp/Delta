[TestFixture]
public abstract class LocalDbTestBase
{
    static SqlInstance sqlInstance = new(
        name: "DeltaTests",
        buildTemplate: async connection =>
        {
            await  DbBuilder.Create(connection);
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"""
                 insert into [Companies] (Id, Content)
                 values ('{Guid.NewGuid()}', 'initial data')
                 """;
            await command.ExecuteNonQueryAsync();
        });

    public Task<SqlDatabase> LocalDb(string? testSuffix = null)
    {
        DeltaExtensions.ClearLsn();
        return sqlInstance.Build(testFile, null, GetName(testSuffix));
    }

    static string GetName(string? testSuffix)
    {
        var test = TestContext.CurrentContext.Test;
        if (testSuffix == null)
        {
            return test.MethodName!;
        }

        return $"{test.MethodName}_{testSuffix}";
    }

    string testFile;

    protected LocalDbTestBase([CallerFilePath] string sourceFile = "") =>
        testFile = GetType().Name;
}