namespace Delta;

public static partial class DeltaExtensions
{
    public static IApplicationBuilder UseDelta<TDbContext>(this IApplicationBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug, bool allowAnonymous = false)
        where TDbContext : DbContext
    {
        var logger = builder.ApplicationServices.GetLogger();
        return builder.Use(
            async (context, next) =>
            {
                if (await HandleRequest<TDbContext>(context, logger, suffix, shouldExecute, logLevel, allowAnonymous))
                {
                    return;
                }

                await next();
            });
    }

    static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug, bool allowAnonymous = false)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilterFactory((filterContext, next) =>
        {
            var logger = filterContext.ApplicationServices.GetLogger();
            return async invocationContext =>
            {
                if (await HandleRequest<TDbContext>(invocationContext.HttpContext, logger, suffix, shouldExecute, logLevel, allowAnonymous))
                {
                    return Results.Empty;
                }

                return await next(invocationContext);
            };
        });

    static Task<bool> HandleRequest<T>(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, bool>? shouldExecute, LogLevel logLevel, bool allowAnonymous = false)
        where T : DbContext =>
        HandleRequest(
            context,
            logger,
            suffix,
            _ => _.RequestServices
                .GetRequiredService<T>()
                .GetLastTimeStamp(),
            shouldExecute,
            logLevel,
            allowAnonymous);

    public static Task<string> GetLastTimeStamp(this DbContext context, Cancel cancel = default)
    {
        var database = context.Database;
        var connection = database.GetDbConnection();
        var transaction = database.CurrentTransaction?.GetDbTransaction();
        return connection.GetLastTimeStamp(transaction, cancel);
    }

    public static ComponentEndpointConventionBuilder UseDelta<TDbContext>(
        this ComponentEndpointConventionBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<ComponentEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static ConnectionEndpointRouteBuilder UseDelta<TDbContext>(
        this ConnectionEndpointRouteBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<ConnectionEndpointRouteBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static ControllerActionEndpointConventionBuilder UseDelta<TDbContext>(
        this ControllerActionEndpointConventionBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static HubEndpointConventionBuilder UseDelta<TDbContext>(
        this HubEndpointConventionBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<HubEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static IHubEndpointConventionBuilder UseDelta<TDbContext>(
        this IHubEndpointConventionBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<IHubEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static PageActionEndpointConventionBuilder UseDelta<TDbContext>(
        this PageActionEndpointConventionBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<PageActionEndpointConventionBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static RouteGroupBuilder UseDelta<TDbContext>(
        this RouteGroupBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteGroupBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);

    public static RouteHandlerBuilder UseDelta<TDbContext>(
        this RouteHandlerBuilder builder,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteHandlerBuilder, TDbContext>(suffix, shouldExecute, logLevel, allowAnonymous);
}
