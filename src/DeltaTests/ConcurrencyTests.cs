public class ConcurrencyTests :
    LocalDbTestBase
{
    [Test]
    public async Task GetLastTimeStamp_ConcurrentAfterReset()
    {
        await using var database = await LocalDb();

        // Several iterations: the race between a losing ResolveQuery task
        // and the caller's own Execute on the same connection is timing
        // dependent, so loop to make reproduction reliable.
        for (var iteration = 0; iteration < 20; iteration++)
        {
            DeltaExtensions.Reset();
            await RunConcurrent(database);
        }
    }

    static async Task RunConcurrent(SqlDatabase database)
    {
        // Closed connections force Execute through its Open/Close branch —
        // the production scenario (EF manages connection lifetime) and
        // the precondition for the race on the static query field.
        const int concurrency = 64;
        var connections = new SqlConnection[concurrency];
        for (var i = 0; i < concurrency; i++)
        {
            connections[i] = new(database.ConnectionString);
        }

        try
        {
            // Synchronous barrier + dedicated threads so all callers really
            // race into ResolveQuery together, rather than being staggered
            // by threadpool dispatch.
            using var barrier = new ManualResetEventSlim(false);
            var tasks = connections
                .Select(connection => Task.Factory.StartNew(
                        async () =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            barrier.Wait();
                            return await connection.GetLastTimeStamp();
                        },
                        Cancel.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default)
                    .Unwrap())
                .ToArray();

            await Task.Delay(50);
            barrier.Set();

            // Task.WhenAll surfaces any exception from the race; the
            // original bug manifested as "The connection is closed" thrown
            // by ExecuteReaderAsync after an orphaned ResolveQuery closed
            // the same connection mid-query.
            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
            {
                IsNotEmpty(result);
            }
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
