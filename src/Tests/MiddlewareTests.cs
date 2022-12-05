using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class MiddlewareTests :
    LocalDbTestBase
{
    [TestCaseSource(nameof(Cases))]
    public async Task Combinations(bool useSuffixFunc, bool useNullSuffixFunc, bool isGet, bool hasIfNoneMatch, bool hasSameIfNoneMatch, bool alreadyHasEtag, bool useShouldExecuteFunc, bool useTrueShouldExecuteFunc)
    {
        var provider = LoggerRecording.Start();
        var httpContext = new DefaultHttpContext();

        var suffixValue = useSuffixFunc && !useNullSuffixFunc ? "suffix" : null;
        var etag = DeltaExtensions.BuildEtag("rowVersion", suffixValue);
        var request = httpContext.Request;
        if (alreadyHasEtag)
        {
            httpContext.Response.Headers.ETag = "existingEtag";
        }

        request.Path = "/path";
        if (isGet)
        {
            request.Method = "GET";
        }
        else
        {
            request.Method = "POST";
        }

        if (hasIfNoneMatch)
        {
            if (hasSameIfNoneMatch)
            {
                request.Headers.IfNoneMatch = etag;
            }
            else
            {
                request.Headers.IfNoneMatch = "diffEtag";
            }
        }

        var notModified = await DeltaExtensions.HandleRequest(
            httpContext,
            provider,
            useSuffixFunc ? _ => suffixValue : null,
            _ => Task.FromResult("rowVersion"),
            useShouldExecuteFunc ? _ => useTrueShouldExecuteFunc : null,
            LogLevel.Information);
        await Verify(new
            {
                notModified,
                httpContext
            })
            .AddScrubber(_ => _.Replace(DeltaExtensions.AssemblyWriteTime, "AssemblyWriteTime"));
    }

    static bool[] bools =
    {
        true,
        false
    };

    public static IEnumerable<object[]> Cases()
    {
        foreach (var useSuffixFunc in bools)
        foreach (var useNullSuffixFunc in bools)
        foreach (var isGet in bools)
        foreach (var hasIfNoneMatch in bools)
        foreach (var hasSameIfNoneMatch in bools)
        foreach (var alreadyHasEtag in bools)
        foreach (var useShouldExecuteFunc in bools)
        foreach (var useTrueShouldExecuteFunc in bools)
        {
            yield return new object[]
            {
                useSuffixFunc,
                useNullSuffixFunc,
                isGet,
                hasIfNoneMatch,
                hasSameIfNoneMatch,
                alreadyHasEtag,
                useShouldExecuteFunc,
                useTrueShouldExecuteFunc
            };
        }
    }
}