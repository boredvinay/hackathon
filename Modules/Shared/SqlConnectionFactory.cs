using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SharedModule;

public sealed class SqlConnectionFactory(IConfiguration cfg) : IDbConnectionFactory
{
    private readonly string _cs = cfg.GetConnectionString("LabelDb")!;
    public DbConnection Create() => new SqlConnection(_cs);
}