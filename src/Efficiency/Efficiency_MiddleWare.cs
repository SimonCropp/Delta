namespace Efficiency;

public static partial class Efficiency
{
    public static IApplicationBuilder UseEfficiency<T>(this IApplicationBuilder builder)
        where T : DbContext
    {
        var assemblyWriteTime = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).Ticks.ToString();
        return builder.Use(async (context, next) =>
        {
            var request = context.Request;
            var response = context.Response;
            var responseHeaders = response.Headers;
            if (request.Method == "GET")
            {
                var data = context.RequestServices.GetRequiredService<T>();
                var rowVersion = await data.GetLastTimeStamp();
                var etag = $"{assemblyWriteTime}-{rowVersion}";
                responseHeaders.Add("ETag", etag);
                if (request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
                {
                    if (ifNoneMatch == etag)
                    {
                        response.StatusCode = 304;
                        return;
                    }
                }
            }

            await next();
        });
    }
}