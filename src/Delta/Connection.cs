namespace Delta;

public record struct Connection(SqlConnection SqlConnection, DbTransaction? DbTransaction = null)
{
    public static implicit operator Connection(SqlConnection connection) => new(connection);
}