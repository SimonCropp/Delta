using System.Security.Claims;

[TestFixture]
public class SuffixClaimsOrderingTests
{
    [Test]
    public void Suffix_WhenNotAuthenticated_ThrowsByDefault()
    {
        Recording.Start();
        var context = new DefaultHttpContext();
        context.Request.Path = "/graphql";
        context.Request.Method = "GET";

        // User is not authenticated (default state)
        var suffixFunc = (HttpContext ctx) =>
        {
            var userData = ctx.User.FindFirst(ClaimTypes.UserData)?.Value;
            var accessGroup = ctx.User.FindFirst("AccessGroup")?.Value;
            return $"{userData}-{accessGroup}";
        };

        // Now throws by default when suffix is provided and user isn't authenticated
        var exception = ThrowsAsync<InvalidOperationException>(async () =>
            await DeltaExtensions.HandleRequest(
                context,
                new RecordingLogger(),
                suffixFunc,
                _ => Task.FromResult("rowVersion"),
                null,
                LogLevel.Information));

        That(exception!.Message, Does.Contain("suffix callback was provided but the user is not authenticated"));
        That(exception.Message, Does.Contain("UseAuthentication"));
        That(exception.Message, Does.Contain("UseAuthorization"));
        That(exception.Message, Does.Contain("middleware pipeline"));
        That(exception.Message, Does.Contain("allowAnonymous: true"));
    }

    [Test]
    public async Task Suffix_WithAuthenticatedUser_DoesNotThrow()
    {
        Recording.Start();
        var context = new DefaultHttpContext();
        context.Request.Path = "/graphql";
        context.Request.Method = "GET";

        // User IS authenticated
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, "user1"),
            new("AccessGroup", "GroupA")
        };
        context.User = new(new ClaimsIdentity(claims, "TestAuth"));

        var suffixFunc = (HttpContext ctx) =>
        {
            var userData = ctx.User.FindFirst(ClaimTypes.UserData)?.Value;
            var accessGroup = ctx.User.FindFirst("AccessGroup")?.Value;
            return $"{userData}-{accessGroup}";
        };

        // Should not throw - user is authenticated
        await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            suffixFunc,
            _ => Task.FromResult("rowVersion"),
            null,
            LogLevel.Information);

        // Verify the suffix was used correctly
        var etag = context.Response.Headers.ETag.ToString();
        That(etag, Does.Contain("user1-GroupA"));
    }

    [Test]
    public async Task Suffix_WithAuthenticatedUser_ReturnsDifferentSuffixes()
    {
        Recording.Start();

        // Simulate what happens when authentication HAS run first
        var user1Etag = await GetEtagForAuthenticatedUser("user1", "GroupA");
        var user2Etag = await GetEtagForAuthenticatedUser("user2", "GroupB");
        var user1AgainEtag = await GetEtagForAuthenticatedUser("user1", "GroupA");

        // Different users get different ETags (correct behavior when auth runs first)
        AreNotEqual(user1Etag, user2Etag);

        // Same user gets same ETag
        AreEqual(user1Etag, user1AgainEtag);
    }

    [Test]
    public async Task Suffix_WithAllowAnonymous_DoesNotThrowEvenIfNotAuthenticated()
    {
        Recording.Start();
        var context = new DefaultHttpContext();
        context.Request.Path = "/graphql";
        context.Request.Method = "GET";

        // User is not authenticated, but allowAnonymous is true
        var suffixFunc = (HttpContext ctx) => "static-suffix";

        // Should not throw - allowAnonymous bypasses the check
        await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            suffixFunc,
            _ => Task.FromResult("rowVersion"),
            null,
            LogLevel.Information,
            allowAnonymous: true);

        var etag = context.Response.Headers.ETag.ToString();
        That(etag, Does.Contain("static-suffix"));
    }

    [Test]
    public async Task NoSuffix_DoesNotThrowEvenIfNotAuthenticated()
    {
        Recording.Start();
        var context = new DefaultHttpContext();
        context.Request.Path = "/graphql";
        context.Request.Method = "GET";

        // User is not authenticated, but no suffix provided
        // Should not throw because there's no suffix callback to worry about

        await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            suffix: null,
            _ => Task.FromResult("rowVersion"),
            null,
            LogLevel.Information);

        // Should complete without exception
        AreEqual(200, context.Response.StatusCode);
    }

    static async Task<string> GetEtagForAuthenticatedUser(string userData, string accessGroup)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/graphql";
        context.Request.Method = "GET";

        // Simulate that authentication middleware HAS already run
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, userData),
            new("AccessGroup", accessGroup)
        };
        context.User = new(new ClaimsIdentity(claims, "TestAuth"));

        var suffixFunc = (HttpContext ctx) =>
        {
            var userDataClaim = ctx.User.FindFirst(ClaimTypes.UserData)?.Value;
            var accessGroupClaim = ctx.User.FindFirst("AccessGroup")?.Value;
            return $"{userDataClaim}-{accessGroupClaim}";
        };

        await DeltaExtensions.HandleRequest(
            context,
            new RecordingLogger(),
            suffixFunc,
            _ => Task.FromResult("rowVersion"),
            null,
            LogLevel.Information);

        return context.Response.Headers.ETag.ToString();
    }
}
