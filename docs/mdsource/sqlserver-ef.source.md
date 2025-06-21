# SQL Server usage with EntityFramework

[![NuGet Status](https://img.shields.io/nuget/v/Delta.EF.svg?label=Delta.EF)](https://www.nuget.org/packages/Delta.EF/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.SqlServer.svg?label=Delta.SqlServer)](https://www.nuget.org/packages/Delta.SqlServer/)

Docs for when using when using the [SQL Server EF Database Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/?tabs=dotnet-core-cli).


## Implementation

include: sqlserver-implemenation


## Timestamp calculation

include: sqlserver-timestamp


## Usage


### Example SQL schema

snippet: Usage.Schema.verified.sql


### Enable row versioning in Entity Framework

snippet: SampleSqlServerDbContext


### Add to WebApplicationBuilder

snippet: UseDeltaSQLServerEF


include: map-group-ef


include: should-execute-ef


include: last-timestamp-ef


include: sqlserver-helpers