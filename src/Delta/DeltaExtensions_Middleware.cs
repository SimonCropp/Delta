namespace Delta;

public static partial class DeltaExtensions
{
    public static IApplicationBuilder UseDelta(this IApplicationBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug, bool allowAnonymous = false)
    {
        getConnection ??= DiscoverConnection;
        var logger = builder.ApplicationServices.GetLogger();

        Task<string> GetTimeStamp(HttpContext context)
        {
            var (connection, transaction) = getConnection(context);
            return connection.GetLastTimeStamp(transaction);
        }

        return builder.Use(
            async (context, next) =>
            {
                if (await HandleRequest(context, logger, suffix, GetTimeStamp, shouldExecute, logLevel, allowAnonymous))
                {
                    return;
                }

                await next();
            });
    }

    static TBuilder UseDelta<TBuilder>(this TBuilder builder, GetConnection? getConnection = null, Func<HttpContext, string?>? suffix = null, Func<HttpContext, bool>? shouldExecute = null, LogLevel logLevel = LogLevel.Debug, bool allowAnonymous = false)
        where TBuilder : IEndpointConventionBuilder
    {
        getConnection ??= DiscoverConnection;

        return builder.AddEndpointFilterFactory((filterContext, next) =>
        {
            var logger = filterContext.ApplicationServices.GetLogger();

            Task<string> GetTimeStamp(HttpContext context)
            {
                var (connection, transaction) = getConnection(context);
                return connection.GetLastTimeStamp(transaction);
            }

            return async invocationContext =>
            {
                if (await HandleRequest(invocationContext.HttpContext, logger, suffix, GetTimeStamp, shouldExecute, logLevel, allowAnonymous))
                {
                    return Results.Empty;
                }

                return await next(invocationContext);
            };
        });
    }

    public static ComponentEndpointConventionBuilder UseDelta(
        this ComponentEndpointConventionBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<ComponentEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static ConnectionEndpointRouteBuilder UseDelta(
        this ConnectionEndpointRouteBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<ConnectionEndpointRouteBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static ControllerActionEndpointConventionBuilder UseDelta(
        this ControllerActionEndpointConventionBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static HubEndpointConventionBuilder UseDelta(
        this HubEndpointConventionBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<HubEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static IHubEndpointConventionBuilder UseDelta(
        this IHubEndpointConventionBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<IHubEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static PageActionEndpointConventionBuilder UseDelta(
        this PageActionEndpointConventionBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<PageActionEndpointConventionBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static RouteGroupBuilder UseDelta(
        this RouteGroupBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<RouteGroupBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);

    public static RouteHandlerBuilder UseDelta(
        this RouteHandlerBuilder builder,
        GetConnection? getConnection = null,
        Func<HttpContext, string?>? suffix = null,
        Func<HttpContext, bool>? shouldExecute = null,
        LogLevel logLevel = LogLevel.Debug,
        bool allowAnonymous = false) =>
        builder.UseDelta<RouteHandlerBuilder>(getConnection, suffix, shouldExecute, logLevel, allowAnonymous);
}
