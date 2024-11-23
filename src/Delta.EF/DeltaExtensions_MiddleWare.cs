namespace Delta;

public static partial class DeltaExtensions
{
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
            logger.Log(level, "Delta {path}: Skipping since request is {method}", path, request.Method);
            return false;
        }

        if (response.Headers.ETag.Count != 0)
        {
            logger.Log(level, "Delta {path}: Skipping since response has an ETag", path);
            return false;
        }

        if (response.IsImmutableCache())
        {
            logger.Log(level, "Delta {path}: Skipping since response has CacheControl=immutable", path);
            return false;
        }

        if (shouldExecute != null && !shouldExecute(context))
        {
            logger.Log(level, "Delta {path}: Skipping since shouldExecute is false", path);
            return false;
        }

        var timeStamp = await getTimeStamp(context);
        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(timeStamp, suffixValue);
        response.Headers.ETag = etag;
        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            logger.Log(level, "Delta {path}: Skipping since request has no If-None-Match", path);
            return false;
        }

        if (ifNoneMatch != etag)
        {
            logger.Log(
                level,
                """
                Delta {path}: Skipping since If-None-Match != ETag
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