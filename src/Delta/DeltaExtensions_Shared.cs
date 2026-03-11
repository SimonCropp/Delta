namespace Delta;

public static partial class DeltaExtensions
{
    public static bool UseResponseDiagnostics { get; set; } = true;

    static StringValues noStore = new("no-store, max-age=0");

    public static void NoStore(this HttpResponse response) =>
        response.Headers[HeaderNames.CacheControl] = noStore;

    static StringValues noCache = new("no-cache");

    public static void NoCache(this HttpResponse response) =>
        response.Headers[HeaderNames.CacheControl] = noCache;

    static StringValues cacheForever = new("public, max-age=31536000, immutable");

    public static void CacheForever(this HttpResponse response) =>
        response.Headers[HeaderNames.CacheControl] = cacheForever;

    static bool IsImmutableCache(this HttpResponse response)
    {
        foreach (var header in response.Headers.CacheControl)
        {
            if (header is null)
            {
                continue;
            }

            if (header.Contains("immutable", StringComparison.OrdinalIgnoreCase))
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

#if !DeltaEF
        InitConnectionTypes();
#endif
    }

    internal static async Task<bool> HandleRequest(HttpContext context, ILogger logger, Func<HttpContext, string?>? suffix, Func<HttpContext, Task<string>> getTimeStamp, Func<HttpContext, bool>? shouldExecute, LogLevel level, bool allowAnonymous = false)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Path.Value!;

        if (!HttpMethods.IsGet(request.Method))
        {
            WriteNo304Header(response, "Request method is not GET", level, logger, path);
            return false;
        }

        if (response.Headers.ETag.Count != 0)
        {
            WriteNo304Header(response, "Response already has ETag", level, logger, path);
            return false;
        }

        if (response.IsImmutableCache())
        {
            WriteNo304Header(response, "Response already has Cache-Control=immutable", level, logger, path);
            return false;
        }

        if (shouldExecute != null &&
            !shouldExecute(context))
        {
            WriteNo304Header(response, "shouldExecute=false", level, logger, path);
            return false;
        }

        if (suffix is not null &&
            !allowAnonymous &&
            context.User.Identity?.IsAuthenticated != true)
        {
            throw new(
                """
                Delta: A suffix callback was provided but the user is not authenticated.
                This usually means UseDelta is registered before UseAuthentication/UseAuthorization in the middleware pipeline.
                Ensure authentication middleware runs before UseDelta so that User claims are available to the suffix callback.
                Example correct ordering:
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseDelta<TDbContext>(suffix: ...);

                If this endpoint intentionally allows anonymous access with a suffix, set allowAnonymous: true.
                """);
        }

        string timeStamp;
        if (TryGetCachedTimeStamp(request, out var cached))
        {
            timeStamp = cached;
            LogTimeStampCacheHit(logger, level, path);
        }
        else
        {
            timeStamp = await getTimeStamp(context);
            timeStampCache = new(timeStamp, Stopwatch.GetTimestamp());
        }

        var suffixValue = suffix?.Invoke(context);
        var etag = BuildEtag(timeStamp, suffixValue);
        response.Headers.ETag = etag;
        LogEtag(logger, level, path, etag);

        if (!request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
        {
            string reason;
            var cacheControl = request.Headers.CacheControl;
            if (cacheControl.Count == 0)
            {
                reason = "Request has no If-None-Match";
            }
            else
            {
                reason = "Request has no If-None-Match. Request Cache-Control header may interfere with caching";
            }

            WriteNo304Header(response, reason, level, logger, path);

            return false;
        }

        if (ifNoneMatch != etag)
        {
            WriteNo304Header(response, "Request If-None-Match != ETag", level, logger, path);
            LogEtagMismatch(logger, level, path, ifNoneMatch.ToString(), etag);

            return false;
        }

        Log304(logger, level, path);

        response.StatusCode = 304;
        response.NoCache();
        return true;
    }

    static void WriteNo304Header(HttpResponse response, string reason, LogLevel level, ILogger logger, string path)
    {
        if (UseResponseDiagnostics)
        {
            response.Headers["Delta-No304"] = reason;
        }

        LogNo304(logger, level, path, reason);
    }

    sealed record TimeStampCache(string Value, long Ticks);

    static TimeStampCache? timeStampCache;

    static bool TryGetCachedTimeStamp(HttpRequest request, [NotNullWhen(true)] out string? timeStamp)
    {
        timeStamp = null;
        var cacheControl = request.Headers.CacheControl;

        if (HasDirective(cacheControl, "no-cache"))
        {
            return false;
        }

        var cache = timeStampCache;
        if (cache is null)
        {
            return false;
        }

        if (!TryParseStaleness(cacheControl, out var maxSeconds))
        {
            return false;
        }

        var elapsed = Stopwatch.GetElapsedTime(cache.Ticks);

        if (maxSeconds != int.MaxValue &&
            elapsed.TotalSeconds > maxSeconds)
        {
            return false;
        }

        if (TryParseDirectiveValue(cacheControl, "min-fresh=", out var minFresh) &&
            elapsed.TotalSeconds + minFresh > maxSeconds)
        {
            return false;
        }

        timeStamp = cache.Value;
        return true;
    }

    // Parses max-age and max-stale from Cache-Control.
    // max-stale without a value means accept any staleness (returns int.MaxValue).
    // If both are present, the larger (more permissive) value wins.
    static bool TryParseStaleness(StringValues cacheControl, out int maxSeconds)
    {
        maxSeconds = 0;
        var found = false;

        foreach (var value in cacheControl)
        {
            if (value is null)
            {
                continue;
            }

            if (TryParseDirectiveValue(value, "max-age=", out var maxAge))
            {
                found = true;
                maxSeconds = Math.Max(maxSeconds, maxAge);
            }

            var staleIndex = value.IndexOf("max-stale", StringComparison.OrdinalIgnoreCase);
            if (staleIndex >= 0)
            {
                found = true;
                var afterDirective = value.AsSpan(staleIndex + 9);
                afterDirective = afterDirective.TrimStart();
                if (afterDirective.Length > 0 &&
                    afterDirective[0] == '=' &&
                    TryParseDirectiveValue(value, "max-stale=", out var maxStale))
                {
                    maxSeconds = Math.Max(maxSeconds, maxStale);
                }
                else
                {
                    // max-stale without a value: accept any staleness
                    maxSeconds = int.MaxValue;
                }
            }
        }

        return found;
    }

    static bool TryParseDirectiveValue(string header, string directive, out int seconds)
    {
        seconds = 0;
        var index = header.IndexOf(directive, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        var span = header.AsSpan(index + directive.Length);
        var end = 0;
        while (end < span.Length && char.IsAsciiDigit(span[end]))
        {
            end++;
        }

        if (end == 0)
        {
            return false;
        }

        return int.TryParse(span[..end], out seconds);
    }

    static bool HasDirective(StringValues cacheControl, string directive)
    {
        foreach (var value in cacheControl)
        {
            if (value is not null &&
                value.Contains(directive, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    static bool TryParseDirectiveValue(StringValues cacheControl, string directive, out int seconds)
    {
        foreach (var value in cacheControl)
        {
            if (value is not null &&
                TryParseDirectiveValue(value, directive, out seconds))
            {
                return true;
            }
        }

        seconds = 0;
        return false;
    }

    [LoggerMessage(Message = "Delta {path}: Using cached timestamp")]
    static partial void LogTimeStampCacheHit(ILogger logger, LogLevel level, string path);

    [LoggerMessage(Message = "Delta {path}: ETag {etag}")]
    static partial void LogEtag(ILogger logger, LogLevel level, string path, string etag);

    [LoggerMessage(Message = "Delta {path}: No 304. {reason}")]
    static partial void LogNo304(ILogger logger, LogLevel level, string path, string reason);

    [LoggerMessage(Message = "Delta {path}: No 304. Request If-None-Match != ETag\nIf-None-Match: {ifNoneMatch}\nETag: {etag}")]
    static partial void LogEtagMismatch(ILogger logger, LogLevel level, string path, string ifNoneMatch, string etag);

    [LoggerMessage(Message = "Delta {path}: 304")]
    static partial void Log304(ILogger logger, LogLevel level, string path);

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
