public class MiddlewareTests :
    LocalDbTestBase
{
    [TestCaseSource(nameof(Cases))]
    public async Task Combinations(bool suffixFunc, bool nullSuffixFunc, bool get, bool ifNoneMatch, bool sameIfNoneMatch, bool etag, bool executeFunc, bool trueExecuteFunc, bool immutable)
    {
        Recording.Start();
        var context = new DefaultHttpContext();

        var suffixValue = suffixFunc && !nullSuffixFunc ? "suffix" : null;
        var request = context.Request;
        var response = context.Response;

        if (etag)
        {
            response.Headers.ETag = "existingEtag";
        }

        request.Path = "/path";
        if (get)
        {
            request.Method = "GET";
        }
        else
        {
            request.Method = "POST";
        }

        if (ifNoneMatch)
        {
            if (sameIfNoneMatch)
            {
                request.Headers.IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", suffixValue);
            }
            else
            {
                request.Headers.IfNoneMatch = "diffEtag";
            }
        }

        if (immutable)
        {
            response.CacheForever();
        }

        var notModified = await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            suffixFunc ? _ => suffixValue : null,
            _ => Task.FromResult("rowVersion"),
            executeFunc ? _ => trueExecuteFunc : null,
            LogLevel.Information);
        await Verify(
                new
                {
                    notModified,
                    context
                })
            .AddScrubber(_ => _.Replace(DeltaExtensions.AssemblyWriteTime, "AssemblyWriteTime"));
    }

    static bool[] bools =
    [
        true,
        false
    ];

    public static IEnumerable<object[]> Cases()
    {
        foreach (var useSuffixFunc in bools)
        foreach (var useNullSuffixFunc in bools)
        foreach (var isGet in bools)
        foreach (var hasIfNoneMatch in bools)
        foreach (var hasImmutableCache in bools)
        foreach (var hasSameIfNoneMatch in bools)
        foreach (var alreadyHasEtag in bools)
        foreach (var useShouldExecuteFunc in bools)
        foreach (var useTrueShouldExecuteFunc in bools)
        {
            yield return
            [
                useSuffixFunc,
                useNullSuffixFunc,
                isGet,
                hasIfNoneMatch,
                hasSameIfNoneMatch,
                alreadyHasEtag,
                useShouldExecuteFunc,
                useTrueShouldExecuteFunc,
                hasImmutableCache
            ];
        }
    }
}