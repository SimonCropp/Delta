namespace Efficiency;

public static partial class Efficiency
{
    static string assemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();

    public static IApplicationBuilder UseEfficiency<T>(this IApplicationBuilder builder)
        where T : DbContext =>
        builder.Use(async (context, next) =>
        {
            if (await HandleRequest<T>(context))
            {
                return;
            }

            await next();
        });

    public static ComponentEndpointConventionBuilder UseEfficiency<TDbContext>(this ComponentEndpointConventionBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ComponentEndpointConventionBuilder, TDbContext>();

    public static ConnectionEndpointRouteBuilder UseEfficiency<TDbContext>(ConnectionEndpointRouteBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ConnectionEndpointRouteBuilder, TDbContext>();

    public static ControllerActionEndpointConventionBuilder UseEfficiency<TDbContext>(this ControllerActionEndpointConventionBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ControllerActionEndpointConventionBuilder, TDbContext>();

    public static HubEndpointConventionBuilder UseEfficiency<TDbContext>(this HubEndpointConventionBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<HubEndpointConventionBuilder, TDbContext>();

    public static IHubEndpointConventionBuilder UseEfficiency<TDbContext>(this IHubEndpointConventionBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<IHubEndpointConventionBuilder, TDbContext>();

    public static PageActionEndpointConventionBuilder UseEfficiency<TDbContext>(this PageActionEndpointConventionBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<PageActionEndpointConventionBuilder, TDbContext>();

    public static RouteGroupBuilder UseEfficiency<TDbContext>(this RouteGroupBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<RouteGroupBuilder, TDbContext>();

    public static RouteHandlerBuilder UseEfficiency<TDbContext>(this RouteHandlerBuilder builder)
        where TDbContext : DbContext =>
        builder.UseEfficiency<RouteHandlerBuilder, TDbContext>();

    public static TBuilder UseEfficiency<TBuilder, TDbContext>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilter(async (invocationContext, next) =>
        {
            if (await HandleRequest<TDbContext>(invocationContext.HttpContext))
            {
                return Results.Empty;
            }

            return await next(invocationContext);
        });

    static async Task<bool> HandleRequest<T>(HttpContext context)
        where T : DbContext
    {
        var request = context.Request;
        var response = context.Response;
        var responseHeaders = response.Headers;
        if (request.Method != "GET")
        {
            return false;
        }

        var data = context.RequestServices.GetRequiredService<T>();
        var rowVersion = await data.GetLastTimeStamp();
        var etag = $"{assemblyWriteTime}-{rowVersion}";
        responseHeaders.Add("ETag", etag);
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            return false;
        }

        if (ifNoneMatch != etag)
        {
            return false;
        }

        response.StatusCode = 304;
        return true;
    }
}