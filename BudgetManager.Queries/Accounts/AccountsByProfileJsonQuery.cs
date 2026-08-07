using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Accounts
{
    /// <summary>
    /// JSON-backed equivalent of <see cref="AccountsByProfileQuery"/>:
    /// returns all accounts belonging to a profile from the local Account.json file.
    /// </summary>
    public class AccountsByProfileJsonQuery : JsonQuery<Account>, IQuery<IEnumerable<Account>, Guid>
    {
        public AccountsByProfileJsonQuery(IEntityStore<Account> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<Account>>> ExecuteQueryAsync(Guid parameter)
        {
            var matches = new List<Account>();
            await foreach (var account in Store.StreamAllAsync())
            {
                if (account.ProfileId == parameter)
                    matches.Add(account);
            }
            return QueryResult<IEnumerable<Account>>.OK(matches);
        }
    }
}
