namespace Delta;

public static partial class DeltaExtensions
{

    internal static string AssemblyWriteTime;

    static DeltaExtensions()
    {
        #region AssemblyWriteTime

        var webAssemblyLocation = Assembly.GetEntryAssembly()!.Location;
        AssemblyWriteTime = File.GetLastWriteTime(webAssemblyLocation).Ticks.ToString();

        #endregion
    }

    public static IApplicationBuilder UseDelta<T>(this IApplicationBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where T : DbContext
    {
        var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Delta");
        return builder.Use(async (context, next) =>
        {
            if (await HandleRequest<T>(context, logger, suffix, shouldExecute, logLevel))
            {
                return;
            }

            await next();
        });
    }

    public static ComponentEndpointConventionBuilder UseDelta<TDbContext>(this ComponentEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ComponentEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static ConnectionEndpointRouteBuilder UseDelta<TDbContext>(ConnectionEndpointRouteBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ConnectionEndpointRouteBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static ControllerActionEndpointConventionBuilder UseDelta<TDbContext>(this ControllerActionEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static HubEndpointConventionBuilder UseDelta<TDbContext>(this HubEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<HubEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static IHubEndpointConventionBuilder UseDelta<TDbContext>(this IHubEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<IHubEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static PageActionEndpointConventionBuilder UseDelta<TDbContext>(this PageActionEndpointConventionBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<PageActionEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static RouteGroupBuilder UseDelta<TDbContext>(this RouteGroupBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteGroupBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static RouteHandlerBuilder UseDelta<TDbContext>(this RouteHandlerBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteHandlerBuilder, TDbContext>(suffix, shouldExecute, logLevel);

    public static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Delta");
            if (await HandleRequest<TDbContext>(context.HttpContext, logger, suffix, shouldExecute, logLevel))
            {
                return Results.Empty;
            }

            return await next(context);
        });

    internal static Task<bool> HandleRequest<T>(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, bool>? shouldExecute, LogLevel logLevel)
        where T : DbContext =>
        HandleRequest(
            context,
            logger,
            suffix,
            _ => _.RequestServices
                .GetRequiredService<T>()
                .GetLastTimeStamp(),
            shouldExecute,
            logLevel);

    internal static async Task<bool> HandleRequest(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, Task<string>> getTimeStamp, Func<HttpContext, bool>? shouldExecute, LogLevel logLevel)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Path;
        if (shouldExecute != null)
        {
            if (!shouldExecute(context))
            {
                logger.Log(logLevel, $"Delta {path}: Skipping since shouldExecute is false");
                return false;
            }
        }

        if (request.Method != "GET")
        {
            logger.Log(logLevel, $"Delta {path}: Skipping since request is {request.Method}");
            return false;
        }

        if (response.Headers.ETag.Any())
        {
            logger.Log(logLevel, $"Delta {path}: Skipping since response has an ETag");
            return false;
        }

        if (IsImmutableCache(response))
        {
            logger.Log(logLevel, $"Delta {path}: Skipping since response has CacheControl=immutable");
            return false;
        }

        var timeStamp = await getTimeStamp(context);
        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(timeStamp, suffixValue);
        response.Headers.Add("ETag", etag);
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            logger.Log(logLevel, $"Delta {path}: Skipping since request has no If-None-Match");
            return false;
        }

        if (ifNoneMatch != etag)
        {
            logger.Log(logLevel, @$"Delta {path}: Skipping since If-None-Match != ETag
If-None-Match: {ifNoneMatch}
ETag: {etag}");
            return false;
        }

        logger.Log(logLevel, $"Delta {path}: 304");
        response.StatusCode = 304;
        return true;
    }

    static bool IsImmutableCache(HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(_ => _ == "immutable"))
            {
                return true;
            }
        }

        return false;
    }

    #region BuildEtag

    internal static string BuildEtag(string timeStamp, string? suffix)
    {
        if (suffix == null)
        {
            return $"\"{AssemblyWriteTime}-{timeStamp}\"";
        }

        return $"\"{AssemblyWriteTime}-{timeStamp}-{suffix}\"";
    }

    #endregion
}