using System.Data.Common;

public interface IDbConnectionFactory
{
    DbConnection Create();
}