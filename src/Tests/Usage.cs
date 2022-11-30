public class Usage : LocalDbTestBase
{
    [Test]
    public async Task LastTimeStamp()
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
}