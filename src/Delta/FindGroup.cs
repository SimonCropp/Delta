using Microsoft.Data.SqlClient;

namespace Delta;

public delegate Connection GetConnection(HttpContext content);

public record struct Connection(SqlConnection SqlConnection, DbTransaction? DbTransaction = null)
{
    public static implicit operator Connection(SqlConnection connection) => new(connection);
}