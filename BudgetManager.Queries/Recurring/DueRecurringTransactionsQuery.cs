using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Recurring
{
    /// <summary>
    /// Returns the enabled recurring transactions that are due for execution on or before
    /// the supplied cut-off date (typically DateTime.Now), earliest first.
    /// </summary>
    public class DueRecurringTransactionsQuery : JsonQuery<RecurringTransaction>, IQuery<IEnumerable<RecurringTransaction>, DateTime>
    {
        public DueRecurringTransactionsQuery(IJsonStore<RecurringTransaction> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<RecurringTransaction>>> ExecuteQueryAsync(DateTime asOf)
        {
            var due = new List<RecurringTransaction>();
            await foreach (var r in Store.StreamAllAsync())
            {
                if (r.Enabled && r.NextExecution.HasValue && r.NextExecution.Value <= asOf)
                    due.Add(r);
            }

            // All entries have a value (filtered above); Nullable.Compare orders them ascending.
            due.Sort((a, b) => Nullable.Compare(a.NextExecution, b.NextExecution));
            return QueryResult<IEnumerable<RecurringTransaction>>.OK(due);
        }
    }
}
