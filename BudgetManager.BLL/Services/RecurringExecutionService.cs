using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// Posts due <see cref="RecurringTransaction"/>s to the transaction ledger.
    ///
    /// A recurring item is "due" when it is enabled and its NextExecution is at or before
    /// the cut-off. For each due occurrence a real Transaction is created dated at the
    /// scheduled time, then NextExecution is advanced by the frequency until it is in the
    /// future. Because the schedule is advanced past the cut-off, running repeatedly (e.g.
    /// on every app load) never double-posts the same occurrence.
    ///
    /// Amount convention (the domain has no explicit type on recurring items):
    ///   Amount &gt;= 0 -> Income, Amount &lt; 0 -> Expense. The posted transaction stores the
    ///   absolute amount.
    ///
    /// Single / Custom frequencies have no interval, so they post once and then disable.
    /// </summary>
    public class RecurringExecutionService
    {
        private const int MaxOccurrencesPerRun = 1000; // safety valve against runaway loops

        private readonly IJsonStore<RecurringTransaction> _recurring;
        private readonly IJsonStore<Transaction> _transactions;

        public RecurringExecutionService(
            IJsonStore<RecurringTransaction> recurring,
            IJsonStore<Transaction> transactions)
        {
            _recurring = recurring;
            _transactions = transactions;
        }

        /// <summary>
        /// Executes all due recurring items at or before <paramref name="asOf"/>.
        /// When <paramref name="accountIds"/> is provided, only items on those accounts run.
        /// Returns the number of transactions posted.
        /// </summary>
        public async Task<int> RunDueAsync(DateTime asOf, ISet<Guid>? accountIds = null)
        {
            var items = await _recurring.ReadAllAsync();
            int created = 0;

            foreach (var r in items)
            {
                if (!r.Enabled || !r.NextExecution.HasValue) continue;
                if (accountIds is not null && !accountIds.Contains(r.AccountId)) continue;

                bool changed = false;
                int guard = 0;

                while (r.Enabled
                       && r.NextExecution.HasValue
                       && r.NextExecution.Value <= asOf
                       && guard++ < MaxOccurrencesPerRun)
                {
                    var occurrence = r.NextExecution.Value;

                    // Past the end date: stop without posting this occurrence, and disable.
                    if (r.EndDate.HasValue && occurrence.Date > r.EndDate.Value.Date)
                    {
                        r.Enabled = false;
                        changed = true;
                        break;
                    }

                    await _transactions.UpsertAsync(BuildTransaction(r, occurrence));
                    created++;
                    changed = true;

                    var next = Advance(r.Frequency, occurrence);
                    if (next is null)
                    {
                        r.Enabled = false; // one-shot (Single / Custom): posted once
                        break;
                    }
                    r.NextExecution = next;

                    // The end-date occurrence was just applied; once the next falls after the
                    // end date, disable the schedule.
                    if (r.EndDate.HasValue && next.Value.Date > r.EndDate.Value.Date)
                    {
                        r.Enabled = false;
                        break;
                    }
                }

                if (changed)
                    await _recurring.UpsertAsync(r);
            }

            return created;
        }

        private static Transaction BuildTransaction(RecurringTransaction r, DateTime date)
        {
            // A destination account makes this a transfer between the user's own accounts.
            if (r.DestinationAccountId.HasValue)
            {
                return new Transaction
                {
                    SourceAccountId = r.AccountId,
                    DestinationAccountId = r.DestinationAccountId,
                    CategoryId = r.CategoryId,
                    Amount = Math.Abs(r.Amount),
                    Type = TransactionType.Transfer,
                    Date = date,
                    Description = $"[Recurring] {r.Name}"
                };
            }

            bool income = r.Amount >= 0m;
            return new Transaction
            {
                SourceAccountId = r.AccountId,
                DestinationAccountId = null,
                CategoryId = r.CategoryId,
                Amount = Math.Abs(r.Amount),
                Type = income ? TransactionType.Income : TransactionType.Expense,
                Date = date,
                Description = $"[Recurring] {r.Name}"
            };
        }

        /// <summary>Next scheduled date, or null for one-shot frequencies (Single / Custom).</summary>
        public static DateTime? Advance(Frequency frequency, DateTime from) => frequency switch
        {
            Frequency.Daily => from.AddDays(1),
            Frequency.Weekly => from.AddDays(7),
            Frequency.Monthly => from.AddMonths(1),
            Frequency.Quarterly => from.AddMonths(3),
            Frequency.Biannual => from.AddMonths(6),
            Frequency.Yearly => from.AddYears(1),
            _ => null
        };
    }
}
