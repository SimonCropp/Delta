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

    [Test]
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var context = database.Context;
        var timeStamp = await context.GetLastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.GetLastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        await database.Connection.EnableTracking();
        var context = database.Context;
        var timeStamp = await context.GetLastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.GetLastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetDatabasesWithTracking()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.EnableTracking();
        Assert.IsNotEmpty(await connection.GetTrackedDatabases());
    }

    [Test]
    public async Task GetTrackedTables()
    {
        var database = await LocalDb();
        var connection = database.Connection;
        await connection.DisableTracking();
        await connection.SetTrackedTables(new []{"Companies"});
        await Verify(connection.GetTrackedTables());
    }

    [Test]
    public async Task DuplicateSetTrackedTables()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables(new []{"Companies"});
        await connection.SetTrackedTables(new []{"Companies"});
    }

    [Test]
    public async Task EmptySetTrackedTables()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables(new string[]{});
    }

    [Test]
    public async Task DisableTracking()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.SetTrackedTables(new []{"Companies"});
        await connection.DisableTracking();
        Assert.IsFalse(await connection.IsTrackingEnabled());
    }

    [Test]
    public async Task IsTrackingEnabled()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.EnableTracking();
        Assert.IsTrue(await connection.IsTrackingEnabled());
    }
}