using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

public class Usage :
    LocalDbTestBase
{
    public static void Suffix(WebApplicationBuilder builder)
    {
        #region Suffix

        var app = builder.Build();
        app.UseDelta(
            getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>(),
            suffix: httpContext => "MySuffix");

        #endregion
    }

    public static void ShouldExecute(WebApplicationBuilder builder)
    {
        #region ShouldExecute

        var app = builder.Build();
        app.UseDelta(
            getConnection: httpContext => httpContext.RequestServices.GetRequiredService<SqlConnection>(),
            shouldExecute: httpContext =>
            {
                var path = httpContext.Request.Path.ToString();
                return path.Contains("match");
            });

        #endregion
    }

    [Test]
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        Recording.Start();
        await using var command = database.Connection.CreateCommand();
        command.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetLastTimeStampDbContext()
    {
        await using var database = await LocalDb();

        var dbContext = database.Context;

        #region GetLastTimeStampDbContext

        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);

        #endregion

        IsNotNull(timeStamp);
    }

    [Test]
    public async Task GetLastTimeStampDbConnection()
    {
        await using var database = await LocalDb();

        var sqlConnection = database.Connection;

        #region GetLastTimeStampDbConnection

        var timeStamp = await sqlConnection.GetLastTimeStamp();

        #endregion

        IsNotNull(timeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        await database.Connection.EnableTracking();
        var context = database.Context;
        var timeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        await using var command = database.Connection.CreateCommand();
        command.CommandText =
            $"""
             insert into [Companies] (Id, Content)
             values ('{Guid.NewGuid()}', 'The company')
             """;
        await command.ExecuteNonQueryAsync();
        var newTimeStamp = await DeltaExtensions.GetLastTimeStamp(database.Connection, null);
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetDatabasesWithTracking()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.EnableTracking();

        #region GetDatabasesWithTracking

        var trackedDatabases = await sqlConnection.GetTrackedDatabases();
        foreach (var db in trackedDatabases)
        {
            Trace.WriteLine(db);
        }

        #endregion

        IsNotEmpty(trackedDatabases);
    }

    [Test]
    public async Task GetTrackedTables()
    {
        var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.DisableTracking();

        #region SetTrackedTables

        await sqlConnection.SetTrackedTables(["Companies"]);

        #endregion

        #region GetTrackedTables

        var trackedTables = await sqlConnection.GetTrackedTables();
        foreach (var db in trackedTables)
        {
            Trace.WriteLine(db);
        }

        #endregion

        await Verify(sqlConnection.GetTrackedTables());
    }

    [Test]
    public async Task DuplicateSetTrackedTables()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(["Companies"]);
        await sqlConnection.SetTrackedTables(["Companies"]);
    }

    [Test]
    public async Task EmptySetTrackedTables()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables([]);
    }

    [Test]
    public async Task DisableTracking()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(["Companies"]);

        #region DisableTracking

        await sqlConnection.DisableTracking();

        #endregion

        IsFalse(await sqlConnection.IsTrackingEnabled());
    }

    [Test]
    public async Task IsTrackingEnabled()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;

        #region EnableTracking

        await sqlConnection.EnableTracking();

        #endregion

        #region IsTrackingEnabled

        var isTrackingEnabled = await sqlConnection.IsTrackingEnabled();

        #endregion

        IsTrue(isTrackingEnabled);
    }
}