### Suffix and Authentication

When using a `suffix` callback that accesses `HttpContext.User` claims, authentication middleware **must** run before `UseDelta`. If `UseDelta` runs first, the User claims won't be populated yet, and all users will get the same cache key.

Delta automatically detects this misconfiguration and throws an `InvalidOperationException` with a helpful message if:
- A `suffix` callback is provided
- The user is not authenticated (`context.User.Identity?.IsAuthenticated != true`)

snippet: SuffixWithAuthEF


### AllowAnonymous

For endpoints that intentionally allow anonymous access but still want to use a suffix for cache differentiation (e.g., based on request headers rather than user claims), use `allowAnonymous: true`:

snippet: AllowAnonymousEF
