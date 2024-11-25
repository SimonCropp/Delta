namespace Delta;

public static partial class DeltaExtensions
{
    public static ComponentEndpointConventionBuilder UseDelta(this ComponentEndpointConventionBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<ComponentEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static ConnectionEndpointRouteBuilder UseDelta(ConnectionEndpointRouteBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<ConnectionEndpointRouteBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static ControllerActionEndpointConventionBuilder UseDelta(this ControllerActionEndpointConventionBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static HubEndpointConventionBuilder UseDelta(this HubEndpointConventionBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<HubEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static IHubEndpointConventionBuilder UseDelta(this IHubEndpointConventionBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<IHubEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static PageActionEndpointConventionBuilder UseDelta(this PageActionEndpointConventionBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<PageActionEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static RouteGroupBuilder UseDelta(this RouteGroupBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<RouteGroupBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static RouteHandlerBuilder UseDelta(this RouteHandlerBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug) =>
        builder.UseDelta<RouteHandlerBuilder>(getConnection, suffix, shouldExecute, logLevel);

    public static IApplicationBuilder UseDelta(this IApplicationBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
    {
        getConnection ??= DiscoverConnection;
        var logger = builder.ApplicationServices.GetLogger();
        return builder.Use(
            async (context, next) =>
            {
                if (await HandleRequest(context, getConnection, logger, suffix, shouldExecute, logLevel))
                {
                    return;
                }

                await next();
            });
    }

    public static TBuilder UseDelta<TBuilder>(this TBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug)
        where TBuilder : IEndpointConventionBuilder
    {
        getConnection ??= DiscoverConnection;

        return builder.AddEndpointFilterFactory((filterContext, next) =>
        {
            var logger = filterContext.ApplicationServices.GetLogger();
            return async invocationContext =>
            {
                if (await HandleRequest(invocationContext.HttpContext, getConnection, logger, suffix, shouldExecute, logLevel))
                {
                    return Results.Empty;
                }

                return await next(invocationContext);
            };
        });
    }

    #region DiscoverConnection

    static Connection DiscoverConnection(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices;
        var connection = provider.GetRequiredService<SqlConnection>();
        var transaction = provider.GetService<SqlTransaction>();
        return new(connection, transaction);
    }

    #endregion

    internal static Task<bool> HandleRequest(
        HttpContext context,
        GetConnection getConnection,
        ILogger logger,
        Func<HttpContext, string?>? suffix,
        Func<HttpContext, bool>? shouldExecute,
        LogLevel logLevel) =>
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