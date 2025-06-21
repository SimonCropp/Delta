# SQL Server usage with EntityFramework

[![NuGet Status](https://img.shields.io/nuget/v/Delta.EF.svg?label=Delta.EF)](https://www.nuget.org/packages/Delta.EF/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.SqlServer.svg?label=Delta.SqlServer)](https://www.nuget.org/packages/Delta.SqlServer/)


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


## GetLastTimeStamp:

snippet: GetLastTimeStampEF


include: sqlserver-helpers