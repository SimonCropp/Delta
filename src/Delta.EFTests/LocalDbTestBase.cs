﻿[TestFixture]
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

    public Task<SqlDatabase<SampleDbContext>> LocalDb(string? testSuffix = null) =>
        sqlInstance.Build(testFile, null, GetName(testSuffix));

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