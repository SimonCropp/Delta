namespace Delta;

public static partial class DeltaExtensions
{
    static Type connectionType;
    static Type transactionType;

    [MemberNotNull(nameof(connectionType))]
    [MemberNotNull(nameof(transactionType))]
    static void InitConnectionTypes()
    {
        #region InitConnectionTypesSqlServer

        var sqlConnectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient");
        if (sqlConnectionType != null)
        {
            connectionType = sqlConnectionType;
            transactionType = sqlConnectionType.Assembly.GetType("Microsoft.Data.SqlClient.SqlTransaction")!;
            return;
        }

        #endregion

        #region InitConnectionTypesPostgres

        var npgsqlConnection = Type.GetType("Npgsql.NpgsqlConnection, Npgsql");
        if (npgsqlConnection != null)
        {
            connectionType = npgsqlConnection;
            transactionType = npgsqlConnection.Assembly.GetType("Npgsql.NpgsqlTransaction")!;
            return;
        }

        #endregion

        throw new("Could not find connection type. Tried Microsoft.Data.SqlClient.SqlConnection and Npgsql.NpgsqlTransaction");
    }

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