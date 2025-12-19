[TestFixture]
public class MiddlewareTests
{
    [TestCase("immutable")]
    [TestCase("IMMUTABLE")]
    [TestCase("Immutable")]
    [TestCase("public, max-age=31536000, IMMUTABLE")]
    public async Task ImmutableCacheControlCaseInsensitive(string cacheControlValue)
    {
        Recording.Start();
        var context = new DefaultHttpContext();
        var request = context.Request;
        var response = context.Response;

        request.Path = "/path";
        request.Method = "GET";
        response.Headers.CacheControl = cacheControlValue;

        var notModified = await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            null,
            _ => Task.FromResult("rowVersion"),
            null,
            LogLevel.Information);

        IsFalse(notModified);
        That(response.Headers["Delta-No304"].ToString(), Does.Contain("immutable"));
    }

    [Test]
    public async Task Combinations([Values] bool suffixFunc, [Values] bool nullSuffixFunc, [Values] bool get, [Values] bool ifNoneMatch, [Values] bool sameIfNoneMatch, [Values] bool etag, [Values] bool executeFunc, [Values] bool trueExecuteFunc, [Values] bool immutable, [Values] bool requestCacheControl)
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

        if (requestCacheControl)
        {
            request.Headers.CacheControl = "no-cache";
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
}