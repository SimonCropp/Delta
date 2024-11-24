using Microsoft.Data.SqlClient;

[TestFixture]
public abstract class LocalDbTestBase
{
    static SqlInstance sqlInstance = new(
        name: "DeltaTests",
        buildTemplate: CreateTable);

    static async Task CreateTable(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE [dbo].[Companies](
            	[Id] [uniqueidentifier] NOT NULL,
            	[RowVersion] [timestamp] NOT NULL,
            	[Content] [nvarchar](max) NULL,
             CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED
            (
            	[Id] ASC
            ) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            CREATE TABLE [dbo].[Employees](
            	[Id] [uniqueidentifier] NOT NULL,
            	[RowVersion] [timestamp] NOT NULL,
            	[CompanyId] [uniqueidentifier] NOT NULL,
            	[Content] [nvarchar](max) NULL,
            	[Age] [int] NOT NULL,
             CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED
            (
            	[Id] ASC
            ) ON [PRIMARY]
            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

            CREATE NONCLUSTERED INDEX [IX_Employees_CompanyId] ON [dbo].[Employees]
            (
            	[CompanyId] ASC
            ) ON [PRIMARY]
            """;
        await command.ExecuteNonQueryAsync();
    }

    public Task<SqlDatabase> LocalDb(string? testSuffix = null)
        => sqlInstance.Build(testFile, null, GetName(testSuffix));

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