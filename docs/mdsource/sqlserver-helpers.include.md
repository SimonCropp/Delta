## Delta.SqlServer

A set of helper methods for working with [SQL Server Change Tracking](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/track-data-changes-sql-server) and [SQL Server Row Versioning](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql)

Nuget: [Delta.SqlServer](https://www.nuget.org/packages/Delta.SqlServer)


### GetDatabasesWithTracking

Get a list of all databases with change tracking enabled.

snippet: GetDatabasesWithTracking

Uses the following SQL:

snippet: GetTrackedDatabasesSql


### GetTrackedTables

Get a list of all tracked tables in database.

snippet: GetTrackedTables

Uses the following SQL:

snippet: GetTrackedTablesSql


### IsTrackingEnabled

Determine if change tracking is enabled for a database.

snippet: IsTrackingEnabled

Uses the following SQL:

snippet: IsTrackingEnabledSql


### EnableTracking

Enable change tracking for a database.

snippet: EnableTracking

Uses the following SQL:

snippet: EnableTrackingSql


### DisableTracking

Disable change tracking for a database and all tables within that database.

snippet: DisableTracking

Uses the following SQL:


#### For disabling tracking on a database:

snippet: DisableTrackingSqlDB


#### For disabling tracking on tables:

snippet: DisableTrackingSqlTable


### SetTrackedTables

Enables change tracking for all tables listed, and disables change tracking for all tables not listed.

snippet: SetTrackedTables

Uses the following SQL:


#### For enabling tracking on a database:

snippet: EnableTrackingSql


#### For enabling tracking on tables:

snippet: EnableTrackingTableSql


#### For disabling tracking on tables:

snippet: DisableTrackingTableSql

