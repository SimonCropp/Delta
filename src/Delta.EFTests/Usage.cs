public class Usage :
    LocalDbTestBase
{
    public static void Suffix(WebApplicationBuilder builder)
    {
        #region SuffixEF

        var app = builder.Build();
        app.UseDelta<SampleDbContext>(suffix: httpContext => "MySuffix");

        #endregion
    }

    public static void ShouldExecute(WebApplicationBuilder builder)
    {
        #region ShouldExecuteEF

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
    public async Task GetLastTimeStamp()
    {
        await using var database = await LocalDb();

        var dbContext = database.Context;

        #region GetLastTimeStampEF

        var timeStamp = await dbContext.GetLastTimeStamp();

        #endregion

        IsNotNull(timeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersion()
    {
        await using var database = await LocalDb();

        var context = database.Context;
        var emptyTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(emptyTimeStamp);
        IsNotNull(emptyTimeStamp);

        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddData(entity);
        var addTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(addTimeStamp);
        IsNotNull(addTimeStamp);
        AreNotEqual(addTimeStamp, emptyTimeStamp);

        entity.Content = "The company2";
        await context.SaveChangesAsync();
        var updateTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(updateTimeStamp);
        IsNotNull(updateTimeStamp);
        AreNotEqual(updateTimeStamp, emptyTimeStamp);
        AreNotEqual(updateTimeStamp, addTimeStamp);

        await context.Companies.ExecuteDeleteAsync();
        var deleteTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(deleteTimeStamp);
        IsNotNull(deleteTimeStamp);
        AreNotEqual(deleteTimeStamp, emptyTimeStamp);
        AreNotEqual(deleteTimeStamp, addTimeStamp);
        AreNotEqual(deleteTimeStamp, updateTimeStamp);
    }

    [Test]
    public async Task LastTimeStampRowVersionAndTracking()
    {
        await using var database = await LocalDb();

        await database.Connection.EnableTracking();
        var context = database.Context;
        var emptyTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(emptyTimeStamp);
        IsNotNull(emptyTimeStamp);

        var entity = new Company
        {
            Content = "The company"
        };
        await database.AddData(entity);
        var addTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(addTimeStamp);
        IsNotNull(addTimeStamp);
        AreNotEqual(addTimeStamp, emptyTimeStamp);

        entity.Content = "The company2";
        await context.SaveChangesAsync();
        var updateTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(updateTimeStamp);
        IsNotNull(updateTimeStamp);
        AreNotEqual(updateTimeStamp, emptyTimeStamp);
        AreNotEqual(updateTimeStamp, addTimeStamp);

        await context.Companies.ExecuteDeleteAsync();
        var deleteTimeStamp = await context.GetLastTimeStamp();
        IsNotEmpty(deleteTimeStamp);
        IsNotNull(deleteTimeStamp);
        AreNotEqual(deleteTimeStamp, emptyTimeStamp);
        AreNotEqual(deleteTimeStamp, addTimeStamp);
        AreNotEqual(deleteTimeStamp, updateTimeStamp);
    }
}