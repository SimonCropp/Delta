### GetLastTimeStamp:

`GetLastTimeStamp` is a helper method to get the DB timestamp that Delta uses to calculate the etag.

It can be called on a DbContext:

snippet: GetLastTimeStampEF

Or a DbConnection:

snippet: GetLastTimeStampConnection