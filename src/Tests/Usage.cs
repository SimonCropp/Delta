using Microsoft.AspNetCore.Builder;

public class Usage :
    LocalDbTestBase
{
    public void Suffix(WebApplicationBuilder builder)
    {
        #region Suffix

        var app = builder.Build();
        app.UseDelta<SampleDbContext>(
            suffix: httpContext => "MySuffix");

        #endregion
    }

    public void ShouldExecute(WebApplicationBuilder builder)
    {
        #region ShouldExecute

        var app = builder.Build();
        app.UseDelta<SampleDbContext>(
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

        var context = database.Context;
        var timeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(newTimeStamp);
        IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetLastTimeStampDbContext()
    {
        await using var database = await LocalDb();

        var dbContext = database.Context;

        #region GetLastTimeStampDbContext

        var timeStamp = await dbContext.GetLastTimeStamp();

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
        var timeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(timeStamp);
        IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.GetLastTimeStamp();
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

        await sqlConnection.SetTrackedTables(
            new[]
            {
                "Companies"
            });

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
        await sqlConnection.SetTrackedTables(new[]
        {
            "Companies"
        });
        await sqlConnection.SetTrackedTables(new[]
        {
            "Companies"
        });
    }

    [Test]
    public async Task EmptySetTrackedTables()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(new string[]
        {
        });
    }

    [Test]
    public async Task DisableTracking()
    {
        await using var database = await LocalDb();
        var sqlConnection = database.Connection;
        await sqlConnection.SetTrackedTables(new[]
        {
            "Companies"
        });

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