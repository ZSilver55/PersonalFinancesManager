using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Transactions
{
    /// <summary>Inclusive date range used to filter transactions.</summary>
    public record DateRange(DateTime From, DateTime To);

    /// <summary>
    /// Returns every transaction whose date falls within the supplied inclusive range,
    /// ordered chronologically.
    /// </summary>
    public class TransactionsByDateRangeQuery : JsonQuery<Transaction>, IQuery<IEnumerable<Transaction>, DateRange>
    {
        public TransactionsByDateRangeQuery(IJsonStore<Transaction> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<Transaction>>> ExecuteQueryAsync(DateRange range)
        {
            var matches = new List<Transaction>();
            await foreach (var t in Store.StreamAllAsync())
            {
                if (t.Date >= range.From && t.Date <= range.To)
                    matches.Add(t);
            }

            matches.Sort((a, b) => a.Date.CompareTo(b.Date));
            return QueryResult<IEnumerable<Transaction>>.OK(matches);
        }
    }
}
