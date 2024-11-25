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

        #region GetLastTimeStampDbContextEF

        var timeStamp = await dbContext.GetLastTimeStamp();

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
}