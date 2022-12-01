using Microsoft.AspNetCore.Http;

public class MiddlewareTests :
    LocalDbTestBase
{
    [TestCaseSource(nameof(Cases))]
    public async Task Combinations(bool useSuffixFunc, bool useNullSuffixFunc, bool isGet, bool hasIfNoneMatch, bool hasSameIfNoneMatch, bool alreadyHasEtag)
    {
        var provider = LoggerRecording.Start();
        var httpContext = new DefaultHttpContext();

        var suffixValue = useSuffixFunc && !useNullSuffixFunc ? "suffix" : null;
        var etag = Delta.Delta.BuildEtag("rowVersion", suffixValue);
        var request = httpContext.Request;
        if (alreadyHasEtag)
        {
            httpContext.Response.Headers.ETag = "existingEtag";
        }

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

        var notModified = await Delta.Delta.HandleRequest(
            httpContext,
            provider,
            useSuffixFunc ? _ => suffixValue : null,
            () => Task.FromResult("rowVersion"));
        await Verify(new
            {
                notModified,
                httpContext
            })
            .AddScrubber(_ => _.Replace(Delta.Delta.AssemblyWriteTime, "AssemblyWriteTime"));
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
        {
            yield return new object[]
            {
                useSuffixFunc, useNullSuffixFunc, isGet, hasIfNoneMatch, hasSameIfNoneMatch, alreadyHasEtag
            };
        }
    }
}