using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Accounts;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    public class AccountController : BaseController<Account>
    {
        QueryHandler<IEnumerable<Account>> _queryHandler;
        public AccountController(CommnadHandler commnadHandler,
            IEntityStore<Account> store,
            QueryHandler<IEnumerable<Account>> queryHandler) : base(commnadHandler, store)
        {
            _queryHandler = queryHandler;
        }
        public async Task<QueryResult<IEnumerable<Account>>> GetAccounts(Guid profileId)
        {
            return await _queryHandler.HandleAsync<Guid>(new AccountsByProfileJsonQuery(_store), profileId);
        }
    }
}
