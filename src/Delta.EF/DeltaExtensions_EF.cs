namespace Delta;

public static partial class DeltaExtensions
{
    public static Task<string> GetLastTimeStamp(this DbContext context, Cancel cancel = default)
    {
        // Do not dispose of this connection as it kills the context
        var database = context.Database;
        var connection = (SqlConnection) database.GetDbConnection();
        var transaction = (SqlTransaction?) database.CurrentTransaction?.GetDbTransaction();
        return GetLastTimeStamp(connection, transaction, cancel);
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

    public static IApplicationBuilder UseDelta<TDbContext>(this IApplicationBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TDbContext : DbContext
    {
        var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Delta");
        return builder.Use(
            async (context, next) =>
            {
                if (await HandleRequest<TDbContext>(context, logger, suffix, shouldExecute, logLevel))
                {
                    return;
                }

                await next();
            });
    }

    public static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilterFactory((filterContext, next) =>
        {
            var loggerFactory = filterContext.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Delta");
            return async invocationContext =>
            {
                if (await HandleRequest<TDbContext>(invocationContext.HttpContext, logger, suffix, shouldExecute, logLevel))
                {
                    return Results.Empty;
                }

                return await next(invocationContext);
            };
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
}