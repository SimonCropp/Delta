namespace Delta;

public static partial class Delta
{
    internal static string AssemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();

    public static IApplicationBuilder UseDelta<T>(this IApplicationBuilder builder, Func<HttpContext, string?>? suffix = null)
        where T : DbContext
    {
        var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Delta");
        return builder.Use(async (context, next) =>
        {
            if (await HandleRequest<T>(context, logger, suffix))
            {
                return;
            }

            await next();
        });
    }

    public static ComponentEndpointConventionBuilder UseDelta<TDbContext>(this ComponentEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ComponentEndpointConventionBuilder, TDbContext>(suffix);

    public static ConnectionEndpointRouteBuilder UseDelta<TDbContext>(ConnectionEndpointRouteBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ConnectionEndpointRouteBuilder, TDbContext>(suffix);

    public static ControllerActionEndpointConventionBuilder UseDelta<TDbContext>(this ControllerActionEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder, TDbContext>(suffix);

    public static HubEndpointConventionBuilder UseDelta<TDbContext>(this HubEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<HubEndpointConventionBuilder, TDbContext>(suffix);

    public static IHubEndpointConventionBuilder UseDelta<TDbContext>(this IHubEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<IHubEndpointConventionBuilder, TDbContext>(suffix);

    public static PageActionEndpointConventionBuilder UseDelta<TDbContext>(this PageActionEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<PageActionEndpointConventionBuilder, TDbContext>(suffix);

    public static RouteGroupBuilder UseDelta<TDbContext>(this RouteGroupBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteGroupBuilder, TDbContext>(suffix);

    public static RouteHandlerBuilder UseDelta<TDbContext>(this RouteHandlerBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteHandlerBuilder, TDbContext>(suffix);

    public static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string?>? suffix = null)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Delta");
            if (await HandleRequest<TDbContext>(context.HttpContext, logger, suffix))
            {
                return Results.Empty;
            }

            return await next(context);
        });

    internal static Task<bool> HandleRequest<T>(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix)
        where T : DbContext =>
        HandleRequest(context, logger, suffix, () =>
        {
            var data = context.RequestServices.GetRequiredService<T>();
            return data.GetLastTimeStamp();
        });

    internal static async Task<bool> HandleRequest(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<Task<string>> getTimeStamp)
    {
        var request = context.Request;
        var response = context.Response;
        if (request.Method != "GET")
        {
            logger.LogInformation($"Skipping since request is {request.Method}");
            return false;
        }

        if (response.Headers.ETag.Any())
        {
            logger.LogInformation("Skipping since response has an ETag");
            return false;
        }

        var rowVersion = await getTimeStamp();
        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(rowVersion, suffixValue);
        response.Headers.Add("ETag", etag);
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            logger.LogInformation("Skipping since request has no If-None-Match");
            return false;
        }

        if (ifNoneMatch != etag)
        {
            logger.LogInformation(@$"Skipping since If-None-Match != ETag
If-None-Match: {ifNoneMatch}
ETag: {etag}");
            return false;
        }

        logger.LogInformation("304");
        response.StatusCode = 304;
        return true;
    }

    internal static string BuildEtag(string rowVersion, string? suffixValue)
    {
        if (suffixValue == null)
        {
            return $"\"{AssemblyWriteTime}-{rowVersion}\"";
        }

        return $"\"{AssemblyWriteTime}-{rowVersion}-{suffixValue}\"";
    }
}