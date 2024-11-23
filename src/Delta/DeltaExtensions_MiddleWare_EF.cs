namespace Delta;

public static partial class DeltaExtensions
{
    public static ComponentEndpointConventionBuilder UseDelta<TDbContext>(this ComponentEndpointConventionBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ComponentEndpointConventionBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static ConnectionEndpointRouteBuilder UseDelta<TDbContext>(ConnectionEndpointRouteBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ConnectionEndpointRouteBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static ControllerActionEndpointConventionBuilder UseDelta<TDbContext>(this ControllerActionEndpointConventionBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static HubEndpointConventionBuilder UseDelta<TDbContext>(this HubEndpointConventionBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<HubEndpointConventionBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static IHubEndpointConventionBuilder UseDelta<TDbContext>(this IHubEndpointConventionBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<IHubEndpointConventionBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static PageActionEndpointConventionBuilder UseDelta<TDbContext>(this PageActionEndpointConventionBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<PageActionEndpointConventionBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static RouteGroupBuilder UseDelta<TDbContext>(this RouteGroupBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteGroupBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static RouteHandlerBuilder UseDelta<TDbContext>(this RouteHandlerBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteHandlerBuilder, TDbContext>(getConnection, suffix, shouldExecute, logLevel);

    public static IApplicationBuilder UseDelta<TDbContext>(this IApplicationBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext
    {
        var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Delta");
        return builder.Use(
            async (context, next) =>
            {
                if (await HandleRequest<TDbContext>(context, getConnection, logger, suffix, shouldExecute, logLevel))
                {
                    return;
                }

                await next();
            });
    }

    public static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, GetConnection getConnection, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilterFactory((filterContext, next) =>
        {
            var loggerFactory = filterContext.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Delta");
            return async invocationContext =>
            {
                if (await HandleRequest<TDbContext>(invocationContext.HttpContext, getConnection, logger, suffix, shouldExecute, logLevel))
                {
                    return Results.Empty;
                }

                return await next(invocationContext);
            };
        });

    internal static Task<bool> HandleRequest<T>(
        HttpContext context,
        GetConnection getConnection,
        ILogger logger,
        Func<HttpContext, string?>? suffix,
        Func<HttpContext, bool>? shouldExecute,
        LogLevel logLevel)
        where T : DbContext =>
        HandleRequest(
            context,
            logger,
            suffix,
            _ =>
            {
                var (connection, transaction) = getConnection(_);
                return GetLastTimeStamp(connection, transaction);
            },
            shouldExecute,
            logLevel);
}