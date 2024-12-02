using System.Diagnostics.CodeAnalysis;

namespace Delta;

public static partial class DeltaExtensions
{

    #region DiscoverConnection

    static Type transactionType;
    static Type connectionType;

    [MemberNotNull(nameof(transactionType))]
    [MemberNotNull(nameof(connectionType))]
    static void FindTypes()
    {
        var sqlConnectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient");
        if (sqlConnectionType != null)
        {
            connectionType = sqlConnectionType;
            transactionType = Type.GetType("Microsoft.Data.SqlClient.SqlTransaction, Microsoft.Data.SqlClient")!;
            return;
        }

        throw new("Could not find connection type. Tried Microsoft.Data.SqlClient.SqlConnection");
    }

    static Connection DiscoverConnection(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices;
        var connection = (DbConnection) provider.GetRequiredService(connectionType);
        var transaction = (DbTransaction?) provider.GetService(transactionType);
        return new(connection, transaction);
    }

    #endregion
}