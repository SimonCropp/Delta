namespace Delta;

public record struct Connection(SqlConnection SqlConnection, SqlTransaction? DbTransaction = null)
{
    public static implicit operator Connection(SqlConnection connection) => new(connection);
}