using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Common.SQL;
using Dapper;
using Microsoft.Extensions.Options;
using System.Data;

namespace BudgetManager.Queries.Accounts
{
    public class AccountsByProfileQuery : BaseRepository, IQueryAll<IEnumerable<Account>>
    {
        Guid _profileId;
        public AccountsByProfileQuery(IDbConnectionFactory dbConnectionFactory,
            IOptions<Settings> options,
            Guid profileId) : base(dbConnectionFactory, options)
        {
            this._profileId = profileId;
        }
        public async Task<QueryResult<IEnumerable<Account>>> ExecuteQueryAsync()
        {
            using IDbConnection connection = OpenConnection();

            var result = await connection.QueryAsync<Account>(
                GetScript(),
                new { profileId = _profileId },
                commandType: CommandType.Text);

            return QueryResult<IEnumerable<Account>>.OK(result);
        }
        protected override string GetScript()
        {
            return "SELECT * FROM dbo.Accounts WHERE profileId = @profileId";
        }
    }
}
