using Npgsql;

namespace Spinoza.Data;

public sealed class DbConnectionFactory(string connectionString)
{
    public NpgsqlConnection Create() => new(connectionString);
}
