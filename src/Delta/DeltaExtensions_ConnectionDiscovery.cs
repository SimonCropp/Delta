namespace Delta;

public static partial class DeltaExtensions
{
    static Type connectionType;
    static Type transactionType;

    #region DiscoverConnection

    static Connection DiscoverConnection(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices;
        var connection = (DbConnection) provider.GetRequiredService(connectionType);
        var transaction = (DbTransaction?) provider.GetService(transactionType);
        return new(connection, transaction);
    }

    #endregion
}