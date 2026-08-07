using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>
    /// Posts due interest for savings accounts as Income transactions, compounding on the
    /// account's current balance (which already reflects posted transactions, including
    /// scheduled transfers). Interest is applied at the account's frequency from NextInterestDate,
    /// advancing it each time. Posting real transactions keeps a trackable, editable ledger.
    /// </summary>
    public class InterestExecutionService
    {
        private const int MaxPerRun = 1000;

        private readonly IEntityStore<Account> _accounts;
        private readonly IEntityStore<Transaction> _transactions;

        public InterestExecutionService(IEntityStore<Account> accounts, IEntityStore<Transaction> transactions)
        {
            _accounts = accounts;
            _transactions = transactions;
        }

        public async Task<int> RunDueAsync(DateTime asOf, ISet<Guid>? accountIds = null)
        {
            var accounts = await _accounts.ReadAllAsync();
            var allTxns = await _transactions.ReadAllAsync();
            int created = 0;

            foreach (var a in accounts)
            {
                if (a.Type != AccountType.Savings) continue;
                if (a.AnnualInterestRate <= 0m || !a.NextInterestDate.HasValue) continue;
                if (accountIds is not null && !accountIds.Contains(a.Id)) continue;

                var periods = PeriodsPerYear(a.InterestFrequency);
                if (periods is null) continue; // Custom / Single have no fixed period

                decimal ratePerPeriod = a.AnnualInterestRate / 100m / periods.Value;

                // Mutable list so newly posted interest compounds into later periods.
                var accountTxns = allTxns
                    .Where(t => t.SourceAccountId == a.Id || t.DestinationAccountId == a.Id)
                    .ToList();

                bool changed = false;
                int guard = 0;
                while (a.NextInterestDate.HasValue && a.NextInterestDate.Value.Date <= asOf.Date && guard++ < MaxPerRun)
                {
                    var date = a.NextInterestDate.Value.Date;
                    decimal balance = BudgetService.ComputeBalance(a, accountTxns.Where(t => t.Date.Date <= date));
                    decimal interest = Math.Round(balance * ratePerPeriod, 2, MidpointRounding.AwayFromZero);

                    if (interest != 0m)
                    {
                        var txn = new Transaction
                        {
                            SourceAccountId = a.Id,
                            DestinationAccountId = null,
                            Amount = Math.Abs(interest),
                            Type = interest >= 0m ? TransactionType.Income : TransactionType.Expense,
                            Date = date,
                            Description = $"[Interest] {a.Name}"
                        };
                        await _transactions.UpsertAsync(txn);
                        accountTxns.Add(txn);
                        created++;
                    }

                    var next = RecurringExecutionService.Advance(a.InterestFrequency, date);
                    if (next is null) break;
                    a.NextInterestDate = next;
                    changed = true;
                }

                if (changed) await _accounts.UpsertAsync(a);
            }

            return created;
        }

        /// <summary>Compounding periods per year for a frequency, or null for non-periodic ones.</summary>
        public static int? PeriodsPerYear(Frequency frequency) => frequency switch
        {
            Frequency.Daily => 365,
            Frequency.Weekly => 52,
            Frequency.Monthly => 12,
            Frequency.Quarterly => 4,
            Frequency.Biannual => 2,
            Frequency.Yearly => 1,
            _ => null // Custom / Single
        };
    }
}
