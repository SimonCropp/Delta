using System.Data.Common;

namespace Delta;

public record struct Connection(DbConnection SqlConnection, DbConnection? DbTransaction = null)
{
    public static implicit operator Connection(DbConnection connection) => new(connection);
}