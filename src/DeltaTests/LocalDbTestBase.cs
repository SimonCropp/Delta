[TestFixture]
public abstract class LocalDbTestBase
{
    private static SqlInstance sqlInstance = new(
        name: "DeltaTests",
        buildTemplate: async connection =>
        {
            await DbBuilder.Create(connection);
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"""
                 insert into [Companies] (Id, Content)
                 values ('{Guid.NewGuid()}', 'initial data')
                 """;
            await command.ExecuteNonQueryAsync();
            await connection.SetTrackedTables(["Companies"]);
        });

    public Task<SqlDatabase> LocalDb(string? suffix = null)
    {
        DeltaExtensions.Reset();
        return sqlInstance.Build(testFile, null, GetName(suffix));
    }

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