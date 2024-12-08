namespace Delta;

public record struct Connection(DbConnection SqlConnection, DbTransaction? DbTransaction = null)
{
    public static implicit operator Connection(DbConnection connection) => new(connection);
}