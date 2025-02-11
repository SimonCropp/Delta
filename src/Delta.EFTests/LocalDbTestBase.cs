[TestFixture]
public abstract class LocalDbTestBase
{
    static SqlInstance<SampleDbContext> sqlInstance;

    static LocalDbTestBase() =>
        sqlInstance = new(
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return new(builder.Options);
            },
            storage: Storage.FromSuffix<SampleDbContext>("ef"));

    public Task<SqlDatabase<SampleDbContext>> LocalDb(string? suffix = null) =>
        sqlInstance.Build(testFile, null, GetName(suffix));

    static string GetName(string? suffix)
    {
        var test = TestContext.CurrentContext.Test;
        var methodName = test.MethodName!;
        var arguments = string.Join(
            ' ',
            test.Arguments.Select(VerifierSettings.GetNameForParameter));

        if (suffix == null)
        {
            return $"{test.MethodName}_{arguments}_{suffix}";
        }

        return $"{methodName}_{arguments}_{suffix}";
    }

    string testFile;

    protected LocalDbTestBase([CallerFilePath] string sourceFile = "") =>
        testFile = GetType().Name;
}