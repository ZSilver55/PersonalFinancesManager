using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Transactions;

namespace BudgetManager.BLL
{
    public class TransactionsController : BaseController<Transaction>
    {
        QueryHandler<IEnumerable<Transaction>> _queryHandler;
        public TransactionsController(CommnadHandler commnadHandler,
            IEntityStore<Transaction> store,
            QueryHandler<IEnumerable<Transaction>> queryHandler) : base(commnadHandler, store)
        {
            _queryHandler = queryHandler;
        }
        public async Task<QueryResult<IEnumerable<Transaction>>> GetTransactionsByAccount(Guid accountId)
        {
            return await _queryHandler.HandleAsync<Guid>(new TransactionsByAccountQuery(_store), accountId);
        }
        public async Task<QueryResult<IEnumerable<Transaction>>> GetTransactionsByDateRange(DateRange dateRange)
        {
            return await _queryHandler.HandleAsync<DateRange>(new TransactionsByDateRangeQuery(_store), dateRange);
        }

    }
}
