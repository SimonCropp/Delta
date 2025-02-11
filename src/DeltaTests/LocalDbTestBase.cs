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

    public Task<SqlDatabase> LocalDb(string? suffix = null) =>
        sqlInstance.Build(testFile, null, GetName(suffix));

    static string GetName(string? suffix)
    {
        var test = TestContext.CurrentContext.Test;
        var method = test.MethodName!;
        var arguments = string.Join(
            ' ',
            test.Arguments.Select(VerifierSettings.GetNameForParameter));

        return $"{method}_{arguments}_{suffix}";
    }

    string testFile;

    protected LocalDbTestBase([CallerFilePath] string sourceFile = "") =>
        testFile = GetType().Name;
}