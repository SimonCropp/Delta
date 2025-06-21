# <img src="/src/icon.png" height="30px"> Delta

[![Build status](https://ci.appveyor.com/api/projects/status/20t96gnsmysklh09/branch/main?svg=true)](https://ci.appveyor.com/project/SimonCropp/Delta)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.svg?label=Delta)](https://www.nuget.org/packages/Delta/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.EF.svg?label=Delta.EF)](https://www.nuget.org/packages/Delta.EF/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.SqlServer.svg?label=Delta.SqlServer)](https://www.nuget.org/packages/Delta.SqlServer/)

include: intro

**See [Milestones](../../milestones?state=closed) for release notes.**


## Sponsors

include: zzz


### JetBrains

[![JetBrains logo.](https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.svg)](https://jb.gg/OpenSourceSupport)


## Assumptions

Frequency of updates to data is relatively low compared to reads


## 304 Not Modified Flow

```mermaid
graph TD
    Request
    CalculateEtag[Calculate current ETag<br/>based on timestamp<br/>from web assembly and SQL]
    IfNoneMatch{Has<br/>If-None-Match<br/>header?}
    EtagMatch{Current<br/>Etag matches<br/>If-None-Match?}
    AddETag[Add current ETag<br/>to Response headers]
    304[Respond with<br/>304 Not-Modified]
    Request --> CalculateEtag
    CalculateEtag --> IfNoneMatch
    IfNoneMatch -->|Yes| EtagMatch
    IfNoneMatch -->|No| AddETag
    EtagMatch -->|No| AddETag
    EtagMatch -->|Yes| 304
```


### SQL Server

include: sqlserver-implemenation


### Postgres

include: postgres-implemenation


## ETag calculation logic

The ETag is calculated from a combination several parts

```
{AssemblyWriteTime}-{DbTimeStamp}-{Suffix}
```


### AssemblyWriteTime

The last write time of the web entry point assembly

snippet: AssemblyWriteTime


### DB timestamp

Timestamp calculation is specific to the target database

 * [SQL Server timestamp calculation](/docs/sqlserver.md#timestamp-calculation)
 * [Postgres timestamp calculation](/docs/postgres.md#timestamp-calculation)


### Suffix

An optional string suffix that is dynamically calculated at runtime based on the current `HttpContext`.

snippet: Suffix


### Combining the above

snippet: BuildEtag


## NuGet

Delta is shipped as two nugets:

 * [Delta](https://nuget.org/packages/Delta/): Delivers functionality using SqlConnection and SqlTransaction.
 * [Delta.EF](https://nuget.org/packages/Delta.EF/): Delivers functionality using [SQL Server EF Database Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/?tabs=dotnet-core-cli).

Only one of the above should be used.


## UseResponseDiagnostics

Response diagnostics is an opt-out feature that includes extra log information in the response headers.

Disable by setting UseResponseDiagnostics to false at startup:

snippet: UseResponseDiagnostics

Response diagnostics headers are prefixed with `Delta-`.

Example Response header when the Request has not `If-None-Match` header.

<img src="/src/Delta-No304.png">


## Verifying behavior

The behavior of Delta can be verified as follows:

 * Open a page in the site
 * Open the browser developer tools
 * Change to the Network tab
 * Refresh the page.

Cached responses will show as 304 in the `Status`:

<img src="/src/network.png">

In the headers `if-none-match` will show in the request and `etag` will show in the response:

<img src="/src/network-details.png">


### Ensure cache is not disabled

If disable cache is checked, the browser will not send the `if-none-match` header. This will effectively cause a cache miss server side, and the full server pipeline will execute.

<img src="/src/disable-cache.png">


### Certificates and Chromium

Chromium, and hence the Chrome and Edge browsers, are very sensitive to certificate problems when determining if an item should be cached. Specifically, if a request is done dynamically (type: xhr) and the server is using a self-signed certificate, then the browser will not send the `if-none-match` header. [Reference]( https://issues.chromium.org/issues/40666473). If self-signed certificates are required during development in lower environment, then use FireFox to test the caching behavior. 


## Programmatic client usage

Delta is primarily designed to support web browsers as a client. All web browsers have the necessary 304 and caching functionally required.

In the scenario where web apis (that support using 304) are being consumed using .net as a client, consider using one of the below extensions to cache responses.

 * [Replicant](https://github.com/SimonCropp/Replicant)
 * [Tavis.HttpCache](https://github.com/tavis-software/Tavis.HttpCache)
 * [CacheCow](https://github.com/aliostad/CacheCow)
 * [Monkey Cache](https://github.com/jamesmontemagno/monkey-cache)


## Icon

[Estuary](https://thenounproject.com/term/estuary/1847616/) designed by [Daan](https://thenounproject.com/Asphaleia/) from [The Noun Project](https://thenounproject.com).