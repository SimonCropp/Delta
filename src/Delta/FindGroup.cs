namespace Delta;

public delegate Connection GetConnection(HttpContext content);

public record struct Connection(DbConnection DbConnection, DbTransaction? DbTransaction = null)
{
    public static implicit operator Connection(DbConnection connection) => new(connection);
}