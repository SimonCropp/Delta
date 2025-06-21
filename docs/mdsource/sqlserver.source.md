# SQL Server usage

[![NuGet Status](https://img.shields.io/nuget/v/Delta.svg?label=Delta)](https://www.nuget.org/packages/Delta/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.SqlServer.svg?label=Delta.SqlServer)](https://www.nuget.org/packages/Delta.SqlServer/)

Docs for when using when using [SQL Server SqlClient](https://github.com/dotnet/SqlClient).


## Implementation

include: sqlserver-implemenation


## Timestamp calculation

include: sqlserver-timestamp


## Usage


### Example SQL schema

snippet: Usage.Schema.verified.sql


### Add to WebApplicationBuilder

snippet: UseDeltaSqlServer

include: map-group


include: should-execute


### Custom Connection discovery

By default, Delta uses `HttpContext.RequestServices` to discover the SqlConnection and SqlTransaction:

snippet: InitConnectionTypesSqlServer

snippet: DiscoverConnection

To use custom connection discovery:

snippet: CustomDiscoveryConnectionSqlServer

To use custom connection and transaction discovery:

snippet: CustomDiscoveryConnectionAndTransactionSqlServer


include: last-timestamp


include: sqlserver-helpers