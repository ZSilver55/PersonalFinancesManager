using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Transactions
{
    /// <summary>
    /// Returns every transaction where the given account is either the source or the
    /// destination, most recent first.
    /// </summary>
    public class TransactionsByAccountQuery : JsonQuery<Transaction>, IQuery<IEnumerable<Transaction>, Guid>
    {
        public TransactionsByAccountQuery(IEntityStore<Transaction> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<Transaction>>> ExecuteQueryAsync(Guid accountId)
        {
            var matches = new List<Transaction>();
            await foreach (var t in Store.StreamAllAsync())
            {
                if (t.SourceAccountId == accountId || t.DestinationAccountId == accountId)
                    matches.Add(t);
            }

            // Sort in place (most recent first) so no second collection is allocated.
            matches.Sort((a, b) => b.Date.CompareTo(a.Date));
            return QueryResult<IEnumerable<Transaction>>.OK(matches);
        }
    }
}
