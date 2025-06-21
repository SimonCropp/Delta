# PostgreSQL usage with EntityFramework

[![NuGet Status](https://img.shields.io/nuget/v/Delta.svg?label=Delta)](https://www.nuget.org/packages/Delta/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.EF.svg?label=Delta.EF)](https://www.nuget.org/packages/Delta.EF/)
[![NuGet Status](https://img.shields.io/nuget/v/Delta.SqlServer.svg?label=Delta.SqlServer)](https://www.nuget.org/packages/Delta.SqlServer/)


## Implementation

include: postgres-implemenation


## Timestamp

snippet: PostgresTimeStamp


## Usage


### Example SQL schema

snippet: PostgresSchema


### DbContext

snippet: SamplePostgresDbContext


### Add to WebApplicationBuilder

snippet: UseDeltaPostgresEF


include: map-group-ef


include: should-execute-ef


### GetLastTimeStamp:

snippet: GetLastTimeStampEF