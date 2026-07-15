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

    public async Task<SqlDatabase> LocalDb(string? suffix = null)
    {
        DeltaExtensions.Reset();
        var database = await sqlInstance.Build(testFile, null, GetName(suffix));
        // LocalDb forces delayed_durability on the template (perf), which flows to every
        // clone and makes sys.dm_db_log_stats.log_end_lsn lag behind un-hardened commits.
        // Delta's LSN change-detection needs the committed LSN, so disable it per database.
        await using var command = database.Connection.CreateCommand();
        command.CommandText = "alter database current set delayed_durability = disabled";
        await command.ExecuteNonQueryAsync();
        return database;
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