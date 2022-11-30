public class Usage :
    LocalDbTestBase
{
    [Test]
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var context = database.Context;
        var timeStamp = await context.LastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.LastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        await database.Connection.EnableTracking();
        var context = database.Context;
        var timeStamp = await context.LastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await context.LastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetDatabasesWithTracking()
    {
        await using var database = await LocalDb();
        var connection = database.Connection;
        await connection.EnableTracking();
        Assert.IsNotEmpty(await connection.GetDatabasesWithTracking());
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