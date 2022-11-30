namespace Efficiency;

public static partial class Efficiency
{
    static string assemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();

    public static IApplicationBuilder UseEfficiency<T>(this IApplicationBuilder builder, Func<HttpContext, string>? suffix = null)
        where T : DbContext =>
        builder.Use(async (context, next) =>
        {
            if (await HandleRequest<T>(context, suffix))
            {
                return;
            }

            await next();
        });

    public static ComponentEndpointConventionBuilder UseEfficiency<TDbContext>(this ComponentEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ComponentEndpointConventionBuilder, TDbContext>(suffix);

    public static ConnectionEndpointRouteBuilder UseEfficiency<TDbContext>(ConnectionEndpointRouteBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ConnectionEndpointRouteBuilder, TDbContext>(suffix);

    public static ControllerActionEndpointConventionBuilder UseEfficiency<TDbContext>(this ControllerActionEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<ControllerActionEndpointConventionBuilder, TDbContext>(suffix);

    public static HubEndpointConventionBuilder UseEfficiency<TDbContext>(this HubEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<HubEndpointConventionBuilder, TDbContext>(suffix);

    public static IHubEndpointConventionBuilder UseEfficiency<TDbContext>(this IHubEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<IHubEndpointConventionBuilder, TDbContext>(suffix);

    public static PageActionEndpointConventionBuilder UseEfficiency<TDbContext>(this PageActionEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<PageActionEndpointConventionBuilder, TDbContext>(suffix);

    public static RouteGroupBuilder UseEfficiency<TDbContext>(this RouteGroupBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<RouteGroupBuilder, TDbContext>(suffix);

    public static RouteHandlerBuilder UseEfficiency<TDbContext>(this RouteHandlerBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseEfficiency<RouteHandlerBuilder, TDbContext>(suffix);

    public static TBuilder UseEfficiency<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string>? suffix = null)
        where TBuilder : IEndpointConventionBuilder
        where TDbContext : DbContext =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            if (await HandleRequest<TDbContext>(context.HttpContext, suffix))
            {
                return Results.Empty;
            }

            return await next(context);
        });

    static async Task<bool> HandleRequest<T>(HttpContext context, Func<HttpContext, string>? suffix)
        where T : DbContext
    {
        var request = context.Request;
        var response = context.Response;
        if (request.Method != "GET")
        {
            return false;
        }

        if (response.Headers.ETag.Any())
        {
            return false;
        }

        var data = context.RequestServices.GetRequiredService<T>();
        var rowVersion = await data.GetLastTimeStamp();
        var etag = $"{assemblyWriteTime}-{rowVersion}";
        if (suffix != null)
        {
            etag += "-" + suffix(context);
        }

        etag = $"\"{etag}\"";
        response.Headers.Add("ETag", etag);
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