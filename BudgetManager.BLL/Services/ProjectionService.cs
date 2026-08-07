using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>One day on the projection timeline.</summary>
    public class ProjectionPoint
    {
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }   // running projected net worth at end of the day
        public decimal Actual { get; set; }     // net of posted transactions on the day (income - expense)
        public decimal Recurring { get; set; }  // net of projected recurring occurrences on the day (signed)
        public decimal Interest { get; set; }   // projected savings interest applied on the day
        public decimal Delta => Actual + Recurring + Interest;
    }

    /// <summary>
    /// Cumulative net (income − expense) attributed to one top-level category across the
    /// window. Values are aligned 1:1 with <see cref="ProjectionSeries.Points"/> (same dates),
    /// starting at 0 and moving as that category's transactions/recurring occur.
    /// </summary>
    public class CategorySeries
    {
        public string Name { get; set; } = "";
        public bool IsUncategorized { get; set; }
        public bool IsInterest { get; set; }
        public IReadOnlyList<decimal> Values { get; set; } = new List<decimal>();
    }

    /// <summary>A one-month projection of net worth for a profile, with per-category breakdown.</summary>
    public class ProjectionSeries
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal StartBalance { get; set; }
        public IReadOnlyList<ProjectionPoint> Points { get; set; } = new List<ProjectionPoint>();
        public IReadOnlyList<CategorySeries> Categories { get; set; } = new List<CategorySeries>();

        public decimal EndBalance => Points.Count > 0 ? Points[^1].Balance : StartBalance;

        /// <summary>Range considering the balance line only (used when categories are hidden).</summary>
        public decimal BalanceMin
        {
            get
            {
                decimal m = Math.Min(StartBalance, EndBalance);
                foreach (var p in Points) m = Math.Min(m, p.Balance);
                return m;
            }
        }

        public decimal BalanceMax
        {
            get
            {
                decimal m = Math.Max(StartBalance, EndBalance);
                foreach (var p in Points) m = Math.Max(m, p.Balance);
                return m;
            }
        }

        /// <summary>Range considering the balance line and all category series.</summary>
        public decimal Min
        {
            get
            {
                decimal m = BalanceMin;
                foreach (var c in Categories) foreach (var v in c.Values) m = Math.Min(m, v);
                return m;
            }
        }

        public decimal Max
        {
            get
            {
                decimal m = BalanceMax;
                foreach (var c in Categories) foreach (var v in c.Values) m = Math.Max(m, v);
                return m;
            }
        }
    }

    /// <summary>
    /// Builds a forward-looking one-month projection of a profile's net worth. Posted
    /// transactions in the window are applied on their dates, and each enabled recurring
    /// item is simulated forward from its NextExecution so its future occurrences are
    /// "injected" into the timeline. Because recurring items are executed only up to now
    /// (advancing NextExecution past today), projected occurrences do not double-count
    /// anything already posted.
    /// </summary>
    public class ProjectionService
    {
        private const int MaxOccurrences = 1000;

        private readonly IEntityStore<Account> _accounts;
        private readonly IEntityStore<Transaction> _transactions;
        private readonly IEntityStore<RecurringTransaction> _recurring;
        private readonly IEntityStore<Category> _categories;

        public ProjectionService(
            IEntityStore<Account> accounts,
            IEntityStore<Transaction> transactions,
            IEntityStore<RecurringTransaction> recurring,
            IEntityStore<Category> categories)
        {
            _accounts = accounts;
            _transactions = transactions;
            _recurring = recurring;
            _categories = categories;
        }

        /// <summary>Projects net worth for the month beginning at <paramref name="start"/>.</summary>
        public async Task<ProjectionSeries> BuildAsync(Guid profileId, DateTime start)
        {
            start = start.Date;
            var end = start.AddMonths(1); // exclusive upper bound: days are [start, end)

            var accounts = (await _accounts.ReadAllAsync())
                .Where(a => a.ProfileId == profileId && !a.IsArchived)
                .ToList();
            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            var txns = (await _transactions.ReadAllAsync())
                .Where(t => accountIds.Contains(t.SourceAccountId)
                            || (t.DestinationAccountId.HasValue && accountIds.Contains(t.DestinationAccountId.Value)))
                .ToList();

            // Category attribution: children roll up into their top-level parent; a null category
            // is bucketed as "uncategorized" (Guid.Empty). Only movements inside the window count.
            var categories = await _categories.ReadAllAsync();
            var topOf = BuildTopLevelMap(categories);
            var nameOf = categories.ToDictionary(c => c.Id, c => c.Name);
            var catByDay = new Dictionary<Guid, Dictionary<DateTime, decimal>>();

            void AddCategory(Guid? categoryId, DateTime day, decimal signed)
            {
                Guid key = categoryId is null
                    ? Guid.Empty
                    : (topOf.TryGetValue(categoryId.Value, out var top) ? top : categoryId.Value);
                if (!catByDay.TryGetValue(key, out var map)) { map = new(); catByDay[key] = map; }
                map[day] = (map.TryGetValue(day, out var v) ? v : 0m) + signed;
            }

            // Projected recurring occurrences by day for everything before the window end.
            // We keep occurrences BEFORE the start too, so they can be carried into the start
            // balance — this is what makes consecutive months line up (end of one == start of
            // the next). Occurrences are simulated from NextExecution, which RunDue has already
            // advanced past posted ones, so there is no double counting with actuals.
            var recurringItems = (await _recurring.ReadAllAsync())
                .Where(x => x.Enabled && accountIds.Contains(x.AccountId))
                .ToList();

            var recurringByDay = new Dictionary<DateTime, decimal>();
            foreach (var r in recurringItems)
            {
                // Transfers between the user's own accounts net to zero for net worth and have no
                // category effect, so they don't contribute to the projection.
                if (r.DestinationAccountId.HasValue) continue;

                var occ = r.NextExecution;
                int guard = 0;
                while (occ.HasValue && occ.Value.Date < end && guard++ < MaxOccurrences)
                {
                    if (r.EndDate.HasValue && occ.Value.Date > r.EndDate.Value.Date)
                        break; // schedule ended: no further occurrences

                    Add(recurringByDay, occ.Value.Date, r.Amount); // signed: >=0 income, <0 expense
                    if (occ.Value.Date >= start)
                        AddCategory(r.CategoryId, occ.Value.Date, r.Amount);
                    var next = RecurringExecutionService.Advance(r.Frequency, occ.Value);
                    if (next is null) break; // one-shot (Single / Custom)
                    occ = next;
                }
            }

            // Balance carried into the window = posted actuals before start + projected recurring before start.
            decimal actualBefore = accounts.Sum(a => BudgetService.ComputeBalance(a, txns.Where(t => t.Date.Date < start)));
            decimal recurringBefore = recurringByDay.Where(kv => kv.Key < start).Sum(kv => kv.Value);
            decimal startBalance = actualBefore + recurringBefore;

            // Posted actual net per day inside the window (transfers between own accounts net to zero).
            var actualByDay = new Dictionary<DateTime, decimal>();
            foreach (var t in txns.Where(t => t.Date.Date >= start && t.Date.Date < end))
            {
                if (t.Type == TransactionType.Income && accountIds.Contains(t.SourceAccountId))
                {
                    Add(actualByDay, t.Date.Date, t.Amount);
                    AddCategory(t.CategoryId, t.Date.Date, t.Amount);
                }
                else if (t.Type == TransactionType.Expense && accountIds.Contains(t.SourceAccountId))
                {
                    Add(actualByDay, t.Date.Date, -t.Amount);
                    AddCategory(t.CategoryId, t.Date.Date, -t.Amount);
                }
            }

            // Projected savings interest, compounded on each account's evolving balance (which
            // includes scheduled transfers/recurring). Interest before the window is carried into
            // the start balance so months stay continuous.
            var (interestByDay, interestBeforeStart) = ProjectInterest(accounts, txns, recurringItems, start, end, DateTime.Today);
            startBalance += interestBeforeStart;

            var points = new List<ProjectionPoint>();
            decimal running = startBalance;
            for (var day = start; day < end; day = day.AddDays(1))
            {
                decimal actual = actualByDay.TryGetValue(day, out var a) ? a : 0m;
                decimal recur = recurringByDay.TryGetValue(day, out var rc) ? rc : 0m;
                decimal interest = interestByDay.TryGetValue(day, out var it) ? it : 0m;

                // Balance at the START of the day (before the day's movements). This makes the
                // first point equal StartBalance, and each day's movement raises/lowers the line
                // going into the next point.
                points.Add(new ProjectionPoint { Date = day, Balance = running, Actual = actual, Recurring = recur, Interest = interest });
                running += actual + recur + interest;
            }

            // Closing point at the window end. Its balance equals EndBalance, which by construction
            // equals the next month's StartBalance — so consecutive months join seamlessly.
            points.Add(new ProjectionPoint { Date = end, Balance = running });

            // Cumulative series per top-level category (and uncategorized), aligned to the points.
            var categorySeries = new List<CategorySeries>();
            foreach (var kv in catByDay)
            {
                var dayMap = kv.Value;
                var values = new List<decimal>(points.Count);
                decimal run = 0m;
                for (var day = start; day < end; day = day.AddDays(1))
                {
                    values.Add(run);
                    run += dayMap.TryGetValue(day, out var v) ? v : 0m;
                }
                values.Add(run); // closing point, matching Points

                bool isUncategorized = kv.Key == Guid.Empty;
                categorySeries.Add(new CategorySeries
                {
                    Name = isUncategorized ? "" : (nameOf.TryGetValue(kv.Key, out var nm) ? nm : ""),
                    IsUncategorized = isUncategorized,
                    Values = values
                });
            }

            // "Gained interest" cumulative series: projected interest plus interest already posted
            // in the window (transactions tagged [Interest]).
            var gainedByDay = new Dictionary<DateTime, decimal>(interestByDay);
            foreach (var t in txns.Where(t => t.Type == TransactionType.Income
                                              && t.Date.Date >= start && t.Date.Date < end
                                              && (t.Description ?? string.Empty).StartsWith("[Interest]", StringComparison.Ordinal)))
            {
                gainedByDay[t.Date.Date] = (gainedByDay.TryGetValue(t.Date.Date, out var gv) ? gv : 0m) + t.Amount;
            }

            if (gainedByDay.Values.Any(v => v != 0m))
            {
                var values = new List<decimal>(points.Count);
                decimal run = 0m;
                for (var day = start; day < end; day = day.AddDays(1))
                {
                    values.Add(run);
                    run += gainedByDay.TryGetValue(day, out var g) ? g : 0m;
                }
                values.Add(run);
                categorySeries.Add(new CategorySeries { IsInterest = true, Values = values });
            }

            return new ProjectionSeries
            {
                Start = start,
                End = end,
                StartBalance = startBalance,
                Points = points,
                Categories = categorySeries
                    .OrderByDescending(s => Math.Abs(s.Values[^1]))
                    .ToList()
            };
        }

        private static void Add(Dictionary<DateTime, decimal> map, DateTime day, decimal amount)
        {
            map[day] = (map.TryGetValue(day, out var v) ? v : 0m) + amount;
        }

        /// <summary>
        /// Simulates each savings account forward and returns the projected interest per day inside
        /// the window plus the total interest accrued before the window start (for continuity).
        /// Each account's balance evolves with posted transactions and projected recurring items
        /// (including transfers), so interest compounds on the real running balance.
        /// </summary>
        private static (Dictionary<DateTime, decimal> byDay, decimal beforeStart) ProjectInterest(
            List<Account> accounts, List<Transaction> txns, List<RecurringTransaction> recurringItems,
            DateTime start, DateTime end, DateTime today)
        {
            var byDay = new Dictionary<DateTime, decimal>();
            decimal beforeStart = 0m;

            var savings = accounts
                .Where(a => a.Type == AccountType.Savings && a.AnnualInterestRate > 0m
                            && a.NextInterestDate.HasValue
                            && InterestExecutionService.PeriodsPerYear(a.InterestFrequency) is not null)
                .ToList();
            if (savings.Count == 0) return (byDay, beforeStart);

            // Simulate from the window start, or from today when viewing a future month (so interest
            // between now and the window start is carried into the start balance).
            DateTime simStart = start <= today ? start : today;

            foreach (var sa in savings)
            {
                int periods = InterestExecutionService.PeriodsPerYear(sa.InterestFrequency)!.Value;
                decimal ratePerPeriod = sa.AnnualInterestRate / 100m / periods;

                decimal balance = BudgetService.ComputeBalance(sa, txns.Where(t => t.Date.Date < simStart));

                // Per-day delta to THIS account (posted + projected recurring) over [simStart, end).
                var accDelta = new Dictionary<DateTime, decimal>();
                foreach (var t in txns.Where(t => t.Date.Date >= simStart && t.Date.Date < end))
                    AddActualAccountDelta(accDelta, sa.Id, t);
                foreach (var r in recurringItems)
                {
                    var occ = r.NextExecution;
                    int guard = 0;
                    while (occ.HasValue && occ.Value.Date < end && guard++ < MaxOccurrences)
                    {
                        if (r.EndDate.HasValue && occ.Value.Date > r.EndDate.Value.Date) break;
                        if (occ.Value.Date >= simStart)
                            AddRecurringAccountDelta(accDelta, sa.Id, r, occ.Value.Date);
                        var next = RecurringExecutionService.Advance(r.Frequency, occ.Value);
                        if (next is null) break;
                        occ = next;
                    }
                }

                // Interest application dates from NextInterestDate, stepping by the frequency.
                var interestDates = new HashSet<DateTime>();
                {
                    var d = sa.NextInterestDate;
                    int guard = 0;
                    while (d.HasValue && d.Value.Date < end && guard++ < MaxOccurrences)
                    {
                        if (d.Value.Date >= simStart) interestDates.Add(d.Value.Date);
                        var next = RecurringExecutionService.Advance(sa.InterestFrequency, d.Value);
                        if (next is null) break;
                        d = next;
                    }
                }

                for (var day = simStart; day < end; day = day.AddDays(1))
                {
                    if (interestDates.Contains(day))
                    {
                        decimal interest = Math.Round(balance * ratePerPeriod, 2, MidpointRounding.AwayFromZero);
                        balance += interest; // compound
                        if (day < start) beforeStart += interest;
                        else byDay[day] = (byDay.TryGetValue(day, out var v) ? v : 0m) + interest;
                    }
                    balance += accDelta.TryGetValue(day, out var dd) ? dd : 0m;
                }
            }

            return (byDay, beforeStart);
        }

        private static void AddActualAccountDelta(Dictionary<DateTime, decimal> map, Guid accountId, Transaction t)
        {
            switch (t.Type)
            {
                case TransactionType.Income:
                    if (t.SourceAccountId == accountId) Add(map, t.Date.Date, t.Amount);
                    break;
                case TransactionType.Expense:
                    if (t.SourceAccountId == accountId) Add(map, t.Date.Date, -t.Amount);
                    break;
                case TransactionType.Transfer:
                    if (t.SourceAccountId == accountId) Add(map, t.Date.Date, -t.Amount);
                    if (t.DestinationAccountId == accountId) Add(map, t.Date.Date, t.Amount);
                    break;
            }
        }

        private static void AddRecurringAccountDelta(Dictionary<DateTime, decimal> map, Guid accountId, RecurringTransaction r, DateTime day)
        {
            if (r.DestinationAccountId.HasValue)
            {
                decimal magnitude = Math.Abs(r.Amount);
                if (r.AccountId == accountId) Add(map, day, -magnitude);
                if (r.DestinationAccountId.Value == accountId) Add(map, day, magnitude);
            }
            else if (r.AccountId == accountId)
            {
                Add(map, day, r.Amount); // signed: >=0 income, <0 expense
            }
        }

        /// <summary>Maps each category id to its top-level ancestor's id (walks ParentCategoryId).</summary>
        private static Dictionary<Guid, Guid> BuildTopLevelMap(IReadOnlyList<Category> categories)
        {
            var byId = categories.ToDictionary(c => c.Id);
            var map = new Dictionary<Guid, Guid>();
            foreach (var c in categories)
            {
                var current = c;
                int guard = 0;
                while (current.ParentCategoryId.HasValue
                       && byId.TryGetValue(current.ParentCategoryId.Value, out var parent)
                       && guard++ < 64)
                {
                    current = parent;
                }
                map[c.Id] = current.Id;
            }
            return map;
        }
    }
}
