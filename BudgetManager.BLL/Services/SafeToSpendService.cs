using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>Result of the "safe to spend today" calculation for a profile.</summary>
    public class SafeToSpendResult
    {
        public decimal NetWorth { get; set; }
        public decimal FutureBills { get; set; }
        public decimal SafetyBuffer { get; set; }
        public decimal GoalDailyReserve { get; set; }
        public int DaysToRefill { get; set; }
        public DateTime? NextIncomeDate { get; set; }   // null => no forecast, using month fallback
        public DateTime HorizonEnd { get; set; }

        public decimal SafePerDay { get; set; }
        public decimal SpentToday { get; set; }
        public decimal RemainingToday { get; set; }

        public bool Overcommitted { get; set; }
        public decimal OvercommittedBy { get; set; }
        public bool MixedCurrencies { get; set; }
    }

    /// <summary>
    /// Computes how much is safe to spend today: net worth minus committed bills before the
    /// next income, minus a safety buffer, spread over the days until that income, then reduced
    /// by any daily reserve needed for goals. Self-correcting — it reads current balances each
    /// time, so overspending lowers tomorrow's figure automatically.
    /// </summary>
    public class SafeToSpendService
    {
        private const int MaxOccurrences = 1000;
        private const int ForecastWindowDays = 400; // how far ahead to look for the next income

        private readonly IJsonStore<Account> _accounts;
        private readonly IJsonStore<Transaction> _transactions;
        private readonly IJsonStore<RecurringTransaction> _recurring;
        private readonly IJsonStore<Goal> _goals;

        public SafeToSpendService(
            IJsonStore<Account> accounts,
            IJsonStore<Transaction> transactions,
            IJsonStore<RecurringTransaction> recurring,
            IJsonStore<Goal> goals)
        {
            _accounts = accounts;
            _transactions = transactions;
            _recurring = recurring;
            _goals = goals;
        }

        public async Task<SafeToSpendResult> ComputeAsync(Guid profileId, decimal safetyBuffer, bool reserveForGoals)
        {
            var today = DateTime.Today;

            var accounts = (await _accounts.ReadAllAsync())
                .Where(a => a.ProfileId == profileId && !a.IsArchived)
                .ToList();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            var txns = (await _transactions.ReadAllAsync())
                .Where(t => accountIds.Contains(t.SourceAccountId)
                            || (t.DestinationAccountId.HasValue && accountIds.Contains(t.DestinationAccountId.Value)))
                .ToList();

            var recurringItems = (await _recurring.ReadAllAsync())
                .Where(x => x.Enabled && accountIds.Contains(x.AccountId))
                .ToList();

            // Full net worth as of today (posted transactions up to and including today).
            decimal netWorth = accounts.Sum(a => BudgetService.ComputeBalance(a, txns.Where(t => t.Date.Date <= today)));

            // Next income: earliest projected recurring income OR future-dated posted income after today.
            DateTime? nextIncome = NextIncomeDate(recurringItems, txns, today);
            DateTime horizonEnd = nextIncome ?? new DateTime(today.Year, today.Month, 1).AddMonths(1);
            if (horizonEnd <= today)
                horizonEnd = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            int daysToRefill = Math.Max(1, (horizonEnd.Date - today).Days);

            // Committed outflows still to come before the refill: projected recurring expenses + posted expenses.
            decimal futureBills = 0m;
            foreach (var r in recurringItems.Where(r => r.Amount < 0m && !r.DestinationAccountId.HasValue))
            {
                var occ = r.NextExecution;
                int guard = 0;
                while (occ.HasValue && occ.Value.Date < horizonEnd && guard++ < MaxOccurrences)
                {
                    if (r.EndDate.HasValue && occ.Value.Date > r.EndDate.Value.Date) break;
                    if (occ.Value.Date > today)
                        futureBills += -r.Amount;
                    var next = RecurringExecutionService.Advance(r.Frequency, occ.Value);
                    if (next is null) break;
                    occ = next;
                }
            }
            futureBills += txns
                .Where(t => t.Type == TransactionType.Expense && accountIds.Contains(t.SourceAccountId)
                            && t.Date.Date > today && t.Date.Date < horizonEnd)
                .Sum(t => t.Amount);

            decimal pool = netWorth - futureBills - safetyBuffer;

            // Daily reserve toward goals with a future due date that aren't met yet.
            decimal goalDailyReserve = 0m;
            if (reserveForGoals)
            {
                foreach (var goal in await _goals.ReadAllAsync())
                {
                    if (!goal.DueDate.HasValue) continue;
                    if (goal.DueDate.Value.Date <= today) continue;      // overdue: excluded (flagged elsewhere)
                    if (goal.CurrentAmount >= goal.TargetAmount) continue;

                    int daysUntilDue = Math.Max(1, (goal.DueDate.Value.Date - today).Days);
                    goalDailyReserve += (goal.TargetAmount - goal.CurrentAmount) / daysUntilDue;
                }
            }

            decimal safePerDay = Math.Max(0m, pool / daysToRefill - goalDailyReserve);

            // Discretionary spending already done today (exclude auto-posted recurring items).
            decimal spentToday = txns
                .Where(t => t.Type == TransactionType.Expense && accountIds.Contains(t.SourceAccountId)
                            && t.Date.Date == today
                            && !(t.Description ?? string.Empty).StartsWith("[Recurring]", StringComparison.Ordinal))
                .Sum(t => t.Amount);

            return new SafeToSpendResult
            {
                NetWorth = netWorth,
                FutureBills = futureBills,
                SafetyBuffer = safetyBuffer,
                GoalDailyReserve = goalDailyReserve,
                DaysToRefill = daysToRefill,
                NextIncomeDate = nextIncome,
                HorizonEnd = horizonEnd,
                SafePerDay = safePerDay,
                SpentToday = spentToday,
                RemainingToday = safePerDay - spentToday,
                Overcommitted = pool < 0m,
                OvercommittedBy = pool < 0m ? -pool : 0m,
                MixedCurrencies = accounts.Select(a => a.Currency).Distinct().Count() > 1
            };
        }

        private static DateTime? NextIncomeDate(
            IEnumerable<RecurringTransaction> recurring, IEnumerable<Transaction> txns, DateTime today)
        {
            DateTime limit = today.AddDays(ForecastWindowDays);
            DateTime? best = null;

            foreach (var r in recurring.Where(r => r.Amount >= 0m && !r.DestinationAccountId.HasValue))
            {
                var occ = r.NextExecution;
                int guard = 0;
                while (occ.HasValue && occ.Value.Date <= limit && guard++ < MaxOccurrences)
                {
                    if (r.EndDate.HasValue && occ.Value.Date > r.EndDate.Value.Date) break;
                    if (occ.Value.Date > today)
                    {
                        if (best is null || occ.Value.Date < best.Value) best = occ.Value.Date;
                        break;
                    }
                    var next = RecurringExecutionService.Advance(r.Frequency, occ.Value);
                    if (next is null) break;
                    occ = next;
                }
            }

            var postedIncome = txns
                .Where(t => t.Type == TransactionType.Income && t.Date.Date > today)
                .Select(t => (DateTime?)t.Date.Date)
                .OrderBy(d => d)
                .FirstOrDefault();
            if (postedIncome is not null && (best is null || postedIncome.Value < best.Value))
                best = postedIncome.Value;

            return best;
        }
    }
}
