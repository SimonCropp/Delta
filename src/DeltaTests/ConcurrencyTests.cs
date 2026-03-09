public class ConcurrencyTests :
    LocalDbTestBase
{
    [Test]
    public async Task GetLastTimeStamp_ConcurrentAfterReset()
    {
        await using var database = await LocalDb();

        // Reset to null so all concurrent callers enter ResolveQuery
        DeltaExtensions.Reset();

        // Open separate connections so concurrent calls don't share a single non-thread-safe SqlConnection
        const int concurrency = 20;
        var connections = new SqlConnection[concurrency];
        for (var i = 0; i < concurrency; i++)
        {
            connections[i] = await database.OpenNewConnection();
        }

        try
        {
            var tasks = connections
                .Select(_ => _.GetLastTimeStamp())
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // All calls should return the same timestamp regardless of the race on the static query field
            var distinct = results.Distinct().ToList();
            That(distinct, Has.Count.EqualTo(1));
            IsNotEmpty(distinct[0]);
        }
        finally
        {
            foreach (var connection in connections)
            {
                await connection.DisposeAsync();
            }
        }
    }
}
