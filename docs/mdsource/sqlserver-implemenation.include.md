For SQL Server the transaction log is used (via [dm_db_log_stats](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-db-log-stats-transact-sql)) if the current user has the `VIEW SERVER STATE` permission.

If `VIEW SERVER STATE` is not allowed then a combination of [Change Tracking](https://learn.microsoft.com/en-us/sql/relational-databases/track-changes/track-data-changes-sql-server) and/or [Row Versioning](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql) is used.

Give the above certain kinds of operations will be detected:

|             | Transaction Log | Change Tracking | Row Versioning | Change Tracking<br>and Row Versioning |
|-------------|:---------------:|:---------------:|:--------------:|:----------------------------------:|
| Insert      |        ✅      |        ✅       |        ✅     |                  ✅                |
| Update      |        ✅      |        ✅       |        ✅     |                  ✅                |
| Hard Delete |        ✅      |        ✅       |        ❌     |                  ✅                |
| Soft Delete |        ✅      |        ✅       |        ✅     |                  ✅                |
| Truncate    |        ✅      |        ❌       |        ❌     |                  ❌                |