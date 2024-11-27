namespace Delta;

public static partial class DeltaExtensions
{
    public static bool IncludeNo304ReasonInResponse { get; set; }
    public static void NoStore(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "no-store, max-age=0");

    public static void NoCache(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "no-cache");

    public static void CacheForever(this HttpResponse response) =>
        response.Headers.Append(HeaderNames.CacheControl, "public, max-age=31536000, immutable");

    static bool IsImmutableCache(this HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header.Contains("immutable"))
            {
                return true;
            }
        }

        return false;
    }

    internal static string AssemblyWriteTime;

    static DeltaExtensions()
    {
        #region AssemblyWriteTime

        var webAssemblyLocation = Assembly.GetEntryAssembly()!.Location;
        AssemblyWriteTime = File.GetLastWriteTime(webAssemblyLocation).Ticks.ToString();

        #endregion
    }

    internal static async Task<bool> HandleRequest(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, Task<string>> getTimeStamp, Func<HttpContext, bool>? shouldExecute, LogLevel level)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Path;

        if (request.Method != "GET")
        {
            WriteNo304Header(response, $"Method={request.Method}");
            logger.Log(level, "Delta {path}: No 304 since request is {method}", path, request.Method);
            return false;
        }

        if (response.Headers.ETag.Count != 0)
        {
            WriteNo304Header(response, $"Existing ETag");
            logger.Log(level, "Delta {path}: No 304 since response has an ETag", path);
            return false;
        }

        if (response.IsImmutableCache())
        {
            WriteNo304Header(response, $"CacheControl=immutable");
            logger.Log(level, "Delta {path}: No 304 since response has CacheControl=immutable", path);
            return false;
        }

        if (shouldExecute != null && !shouldExecute(context))
        {
            WriteNo304Header(response, $"shouldExecute=false");
            logger.Log(level, "Delta {path}: No 304 since shouldExecute is false", path);
            return false;
        }

        var timeStamp = await getTimeStamp(context);
        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(timeStamp, suffixValue);
        response.Headers.ETag = etag;
        logger.Log(level, "Delta {path}: ETag {etag}", path, etag);
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            WriteNo304Header(response, $"missing If-None-Match");
            logger.Log(level, "Delta {path}: No 304 since request has no If-None-Match", path);
            return false;
        }

        if (ifNoneMatch != etag)
        {
            WriteNo304Header(response, $"If-None-Match != ETag");
            logger.Log(
                level,
                """
                Delta {path}: No 304 since If-None-Match != ETag
                If-None-Match: {ifNoneMatch}
                ETag: {etag}
                """,
                path,
                ifNoneMatch,
                etag);
            return false;
        }

        logger.Log(level, "Delta {path}: 304", path);
        response.StatusCode = 304;
        response.NoCache();
        return true;
    }

    static void WriteNo304Header(this HttpResponse response, string header)
    {
        if (IncludeNo304ReasonInResponse)
        {
            response.Headers["Delta.No304"] = header;
        }
    }

    static ILogger GetLogger(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger("Delta");
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