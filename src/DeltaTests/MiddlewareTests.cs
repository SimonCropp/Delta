[NotInParallel]
public class MiddlewareTests
{
    [Test]
    [Arguments("immutable")]
    [Arguments("IMMUTABLE")]
    [Arguments("Immutable")]
    [Arguments("public, max-age=31536000, IMMUTABLE")]
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

        await Assert.That(notModified).IsFalse();
        await Assert.That(response.Headers["Delta-No304"].ToString()).Contains("immutable");
    }

    [Test]
    [MatrixDataSource]
    public async Task Combinations([Matrix] bool suffixFunc, [Matrix] bool nullSuffixFunc, [Matrix] bool get, [Matrix] bool ifNoneMatch, [Matrix] bool sameIfNoneMatch, [Matrix] bool etag, [Matrix] bool executeFunc, [Matrix] bool trueExecuteFunc, [Matrix] bool immutable, [Matrix] bool noCache, [Matrix] bool minFresh)
    {
        Recording.Start();
        DeltaExtensions.Reset();
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

        var cacheDirectives = new List<string>();
        if (noCache)
        {
            cacheDirectives.Add("no-cache");
        }

        if (minFresh)
        {
            cacheDirectives.Add("min-fresh=5");
        }

        if (cacheDirectives.Count > 0)
        {
            request.Headers.CacheControl = string.Join(", ", cacheDirectives);
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
            LogLevel.Information,
            allowAnonymous: true);
        await Verify(
                new
                {
                    notModified,
                    context
                })
            .ScrubReplace(DeltaExtensions.AssemblyWriteTime, "AssemblyWriteTime")
            .IgnoreMember("Id");
    }

    [Test]
    public async Task MaxAge_UsesCachedTimeStamp()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: no max-age, populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", null)
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: with max-age, should use cached timestamp
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-age=10",
                    IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", null)
                }
            }
        };

        var notModified = await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // DB was NOT called again — cached timestamp used
        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(notModified).IsTrue();
    }

    [Test]
    public async Task MaxAge_ExpiredCache_QueriesDb()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: max-age=0 means must be fresh
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-age=0"
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // max-age=0 requires fresh data, so DB was called again
        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task NoMaxAge_AlwaysQueriesDb()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // Two requests without max-age
        for (var i = 0; i < 2; i++)
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Path = "/path",
                    Method = "GET"
                }
            };

            await DeltaExtensions.HandleRequest(
                context,
                new RecordingLogger(),
                null,
                GetTimeStamp,
                null,
                LogLevel.Information);
        }

        // Both requests hit the DB
        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task MaxStale_UsesCachedTimeStamp()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: max-stale=10, should use cached timestamp
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-stale=10"
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task MaxStaleNoValue_UsesCachedTimeStamp()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: max-stale without a value means accept any staleness
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-stale"
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task NoCache_BypassesCachedTimeStamp()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: no-cache forces fresh timestamp
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "no-cache",
                    IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", null)
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // no-cache bypasses cache, DB was called again
        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task NoCache_WithMaxAge_StillQueriesDb()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: no-cache wins over max-age
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "no-cache, max-age=3600",
                    IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", null)
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // no-cache takes precedence, DB was called again
        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task MinFresh_SufficientFreshness_UsesCachedTimeStamp()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: max-age=3600, min-fresh=5 — plenty of freshness remaining
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-age=3600, min-fresh=5",
                    IfNoneMatch = DeltaExtensions.BuildEtag("rowVersion", null)
                }
            }
        };

        var notModified = await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // Cached timestamp used — enough freshness remaining
        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(notModified).IsTrue();
    }

    [Test]
    public async Task MinFresh_InsufficientFreshness_QueriesDb()
    {
        Recording.Start();
        DeltaExtensions.Reset();
        var callCount = 0;

        Task<string> GetTimeStamp(HttpContext _)
        {
            callCount++;
            return Task.FromResult("rowVersion");
        }

        // First request: populates the cache
        var context1 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET"
            }
        };

        await DeltaExtensions.HandleRequest(
            context1,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        await Assert.That(callCount).IsEqualTo(1);

        // Second request: max-age=1, min-fresh=3600 — min-fresh exceeds max-age so cache is rejected
        var context2 = new DefaultHttpContext
        {
            Request =
            {
                Path = "/path",
                Method = "GET",
                Headers =
                {
                    CacheControl = "max-age=1, min-fresh=3600"
                }
            }
        };

        await DeltaExtensions.HandleRequest(
            context2,
            new RecordingLogger(),
            null,
            GetTimeStamp,
            null,
            LogLevel.Information);

        // min-fresh requirement not met, DB was called again
        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task CacheControlExtensions()
    {
        var context = new DefaultHttpContext();
        var response = context.Response;

        response.NoStore();
        await Assert.That(response.Headers.CacheControl.ToString()).IsEqualTo("no-store, max-age=0");

        response.NoCache();
        await Assert.That(response.Headers.CacheControl.ToString()).IsEqualTo("no-cache");

        response.CacheForever();
        await Assert.That(response.Headers.CacheControl.ToString()).IsEqualTo("public, max-age=31536000, immutable");
    }

    [Test]
    public async Task ConnectionImplicitOperator()
    {
        using var dbConnection = new SqlConnection();
        Connection connection = dbConnection;

        await Assert.That(connection.SqlConnection).IsEqualTo(dbConnection);
        await Assert.That(connection.DbTransaction).IsNull();
    }
}
