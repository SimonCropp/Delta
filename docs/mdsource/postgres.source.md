# PostgreSQL usage

[![NuGet Status](https://img.shields.io/nuget/v/Delta.svg?label=Delta)](https://www.nuget.org/packages/Delta/)

Docs for when using when using [PostgreSQL Npgsql](https://www.npgsql.org).


## Implementation

include: postgres-implemenation


## Timestamp calculation

snippet: PostgresTimeStamp


## Usage


### Example SQL schema

snippet: PostgresSchema


### Add to WebApplicationBuilder

snippet: UseDeltaPostgres


include: map-group


include: should-execute


### Custom Connection discovery

By default, Delta uses `HttpContext.RequestServices` to discover the NpgsqlConnection and NpgsqlTransaction:

snippet: InitConnectionTypesPostgres

snippet: DiscoverConnection

To use custom connection discovery:

snippet: CustomDiscoveryConnectionPostgres

To use custom connection and transaction discovery:

snippet: CustomDiscoveryConnectionAndTransactionPostgres


include: last-timestamp