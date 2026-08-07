using Microsoft.Data.SqlClient;
using System.Data;

namespace BudgetManager.Queries.Common.SQL
{
    public class SQLDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection Create(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "Connection string cannot be empty.", nameof(connectionString));
            }

            return new SqlConnection(connectionString);
        }
    }
}
