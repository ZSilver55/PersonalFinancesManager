using System.Data;

namespace BudgetManager.Queries.Common.SQL
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create(string connectionString);
    }
}
