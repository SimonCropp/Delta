### Custom Connection discovery

By default, Delta uses `HttpContext.RequestServices` to discover the DbConnection and DbTransaction:

snippet: DiscoverConnection

To use custom connection discovery:

snippet: CustomDiscoveryConnection

To use custom connection and transaction discovery:

snippet: CustomDiscoveryConnectionAndTransaction