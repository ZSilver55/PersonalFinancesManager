using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL.Services
{
    /// <summary>An account paired with its computed current balance.</summary>
    public class AccountBalance
    {
        public Account Account { get; set; } = default!;
        public decimal Balance { get; set; }
    }

    /// <summary>Aggregated figures for the dashboard of a single profile over a period.</summary>
    public class DashboardSummary
    {
        public decimal NetWorth { get; set; }
        public decimal PeriodIncome { get; set; }
        public decimal PeriodExpense { get; set; }
        public decimal PeriodNet => PeriodIncome - PeriodExpense;
        public int AccountCount { get; set; }
        public IReadOnlyList<AccountBalance> Balances { get; set; } = new List<AccountBalance>();
        public IReadOnlyList<Goal> Goals { get; set; } = new List<Goal>();
    }

    /// <summary>
    /// Business rules that span multiple aggregates: current account balances derived
    /// from the transaction ledger, net worth, and period income/expense.
    /// </summary>
    public class BudgetService
    {
        private readonly IEntityStore<Account> _accounts;
        private readonly IEntityStore<Transaction> _transactions;
        private readonly IEntityStore<Goal> _goals;

        public BudgetService(
            IEntityStore<Account> accounts,
            IEntityStore<Transaction> transactions,
            IEntityStore<Goal> goals)
        {
            _accounts = accounts;
            _transactions = transactions;
            _goals = goals;
        }

        /// <summary>
        /// Applies the ledger to a single account:
        ///   Income  -> credited to the source account.
        ///   Expense -> debited from the source account.
        ///   Transfer-> debited from source, credited to destination.
        /// </summary>
        public static decimal ComputeBalance(Account account, IEnumerable<Transaction> transactions)
        {
            decimal balance = account.InitialBalance;

            foreach (var t in transactions)
            {
                switch (t.Type)
                {
                    case TransactionType.Income:
                        if (t.SourceAccountId == account.Id) balance += t.Amount;
                        break;
                    case TransactionType.Expense:
                        if (t.SourceAccountId == account.Id) balance -= t.Amount;
                        break;
                    case TransactionType.Transfer:
                        if (t.SourceAccountId == account.Id) balance -= t.Amount;
                        if (t.DestinationAccountId == account.Id) balance += t.Amount;
                        break;
                }
            }

            return balance;
        }

        /// <summary>Computes the current balances for every (non-archived) account of a profile.</summary>
        public async Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid profileId)
        {
            var accounts = (await _accounts.ReadAllAsync())
                .Where(a => a.ProfileId == profileId)
                .ToList();

            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            var relevant = (await _transactions.ReadAllAsync())
                .Where(t => accountIds.Contains(t.SourceAccountId)
                            || (t.DestinationAccountId.HasValue && accountIds.Contains(t.DestinationAccountId.Value)))
                .ToList();

            return accounts
                .Select(a => new AccountBalance { Account = a, Balance = ComputeBalance(a, relevant) })
                .ToList();
        }

        /// <summary>Builds the dashboard summary for a profile over the given inclusive period.</summary>
        public async Task<DashboardSummary> GetDashboardAsync(Guid profileId, DateTime from, DateTime to)
        {
            var accounts = (await _accounts.ReadAllAsync())
                .Where(a => a.ProfileId == profileId)
                .ToList();

            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            var relevant = (await _transactions.ReadAllAsync())
                .Where(t => accountIds.Contains(t.SourceAccountId)
                            || (t.DestinationAccountId.HasValue && accountIds.Contains(t.DestinationAccountId.Value)))
                .ToList();

            var balances = accounts
                .Where(a => !a.IsArchived)
                .Select(a => new AccountBalance { Account = a, Balance = ComputeBalance(a, relevant) })
                .ToList();

            var period = relevant.Where(t => t.Date >= from && t.Date <= to).ToList();

            decimal income = period
                .Where(t => t.Type == TransactionType.Income && accountIds.Contains(t.SourceAccountId))
                .Sum(t => t.Amount);

            decimal expense = period
                .Where(t => t.Type == TransactionType.Expense && accountIds.Contains(t.SourceAccountId))
                .Sum(t => t.Amount);

            var goals = await _goals.ReadAllAsync();

            return new DashboardSummary
            {
                NetWorth = balances.Sum(b => b.Balance),
                PeriodIncome = income,
                PeriodExpense = expense,
                AccountCount = balances.Count,
                Balances = balances,
                Goals = goals
            };
        }
    }
}
