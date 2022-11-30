namespace Delta;

public static partial class Delta
{
    static string assemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();

    public static IApplicationBuilder UseDelta<T>(this IApplicationBuilder builder, Func<HttpContext, string>? suffix = null)
        where T : DbContext =>
        builder.Use(async (context, next) =>
        {
            if (await HandleRequest<T>(context, suffix))
            {
                return;
            }

            await next();
        });

    public static ComponentEndpointConventionBuilder UseDelta<TDbContext>(this ComponentEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ComponentEndpointConventionBuilder, TDbContext>(suffix);

    public static ConnectionEndpointRouteBuilder UseDelta<TDbContext>(ConnectionEndpointRouteBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ConnectionEndpointRouteBuilder, TDbContext>(suffix);

    public static ControllerActionEndpointConventionBuilder UseDelta<TDbContext>(this ControllerActionEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<ControllerActionEndpointConventionBuilder, TDbContext>(suffix);

    public static HubEndpointConventionBuilder UseDelta<TDbContext>(this HubEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<HubEndpointConventionBuilder, TDbContext>(suffix);

    public static IHubEndpointConventionBuilder UseDelta<TDbContext>(this IHubEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<IHubEndpointConventionBuilder, TDbContext>(suffix);

    public static PageActionEndpointConventionBuilder UseDelta<TDbContext>(this PageActionEndpointConventionBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<PageActionEndpointConventionBuilder, TDbContext>(suffix);

    public static RouteGroupBuilder UseDelta<TDbContext>(this RouteGroupBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteGroupBuilder, TDbContext>(suffix);

    public static RouteHandlerBuilder UseDelta<TDbContext>(this RouteHandlerBuilder builder, Func<HttpContext, string>? suffix = null)
        where TDbContext : DbContext =>
        builder.UseDelta<RouteHandlerBuilder, TDbContext>(suffix);

    public static TBuilder UseDelta<TBuilder, TDbContext>(this TBuilder builder, Func<HttpContext, string>? suffix = null)
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

        response.Headers.Add("ETag", $"\"{etag}\"");
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