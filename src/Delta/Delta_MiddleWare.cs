namespace Delta;

public static partial class Delta
{
    static string assemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();

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

    static async Task<bool> HandleRequest<T>(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix)
        where T : DbContext
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

        var data = context.RequestServices.GetRequiredService<T>();
        var rowVersion = await data.GetLastTimeStamp();
        var etag = $"{assemblyWriteTime}-{rowVersion}";
        var suffixValue = suffix?.Invoke(context);
        if (suffixValue != null)
        {
            etag += "-" + suffixValue;
        }

        response.Headers.Add("ETag", $"\"{etag}\"");
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
}