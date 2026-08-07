using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;
using BudgetManager.Queries.Recurring;

namespace BudgetManager.BLL
{
    public class RecurringTransactionsController : BaseController<RecurringTransaction>
    {
        QueryHandler<IEnumerable<RecurringTransaction>> _queryHandler;
        public RecurringTransactionsController(CommnadHandler commnadHandler,
            IEntityStore<RecurringTransaction> store,
            QueryHandler<IEnumerable<RecurringTransaction>> queryHandler) : base(commnadHandler, store)
        {
            _queryHandler = queryHandler;
        }
        public async Task<QueryResult<IEnumerable<RecurringTransaction>>> GetDueRecurringTransactions(DateTime asOf)
        {
            return await _queryHandler.HandleAsync<DateTime>(new DueRecurringTransactionsQuery(_store), asOf);
        }
    }
}
