using System.Security.Claims;

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

    public static void SuffixWithAuth(WebApplicationBuilder builder)
    {
        #region SuffixWithAuthEF

        var app = builder.Build();

        // Authentication middleware must run before UseDelta
        // so that User claims are available to the suffix callback
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseDelta<SampleDbContext>(
            suffix: httpContext =>
            {
                // Access user claims to create per-user cache keys
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantId = httpContext.User.FindFirst("TenantId")?.Value;
                return $"{userId}-{tenantId}";
            });

        #endregion
    }

    public static void AllowAnonymous(WebApplicationBuilder builder)
    {
        #region AllowAnonymousEF

        var app = builder.Build();

        // For endpoints that intentionally allow anonymous access
        // but still want a suffix for cache differentiation
        app.UseDelta<SampleDbContext>(
            suffix: httpContext => httpContext.Request.Headers["X-Client-Version"].ToString(),
            allowAnonymous: true);

        #endregion
    }

    [Test]
    [TestCase(false)]
    public async Task GetLastTimeStamp([Values] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }

        var dbContext = database.Context;

        #region GetLastTimeStampEF

        var timeStamp = await dbContext.GetLastTimeStamp();

        #endregion

        IsNotNull(timeStamp);
    }

    [Test]
    public async Task LastTimeStamp([Values] bool tracking)
    {
        await using var database = await LocalDb();
        if (tracking)
        {
            await database.Connection.EnableTracking();
        }
        var context = database.Context;
        //seed with an entity so there is something in transaction log
        await database.AddData(
            new Company
            {
                Content = "The company"
            });
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
        AreNotEqual(updateTimeStamp, addTimeStamp);
        AreNotEqual(updateTimeStamp, emptyTimeStamp);
    }
}