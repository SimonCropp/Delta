public class Usage :
    LocalDbTestBase
{
    [Test]
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var timeStamp = await database.Context.LastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await database.Context.LastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        await database.Connection.EnableTracking();
        var timeStamp = await database.Context.LastTimeStamp();
        Assert.IsNotEmpty(timeStamp);
        Assert.IsNotNull(timeStamp);
        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddDataUntracked(entity);
        var newTimeStamp = await database.Context.LastTimeStamp();
        Assert.IsNotEmpty(newTimeStamp);
        Assert.IsNotNull(newTimeStamp);
    }

    [Test]
    public async Task GetDatabasesWithTracking()
    {
        await using var database = await LocalDb();
        await database.Connection.EnableTracking();
        Assert.IsNotEmpty(await database.Connection.GetDatabasesWithTracking());
    }

    [Test]
    public async Task DisableTracking()
    {
        await using var database = await LocalDb();
        await database.Connection.EnableTracking();
        await database.Connection.DisableTracking();
        Assert.IsFalse(await database.Connection.IsTrackingEnabled());
    }

    [Test]
    public async Task IsTrackingEnabled()
    {
        await using var database = await LocalDb();
        await database.Connection.EnableTracking();
        Assert.IsTrue(await database.Connection.IsTrackingEnabled());
    }
}