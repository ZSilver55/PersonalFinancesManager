using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Categories;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    public class CategoriesController : BaseController<Category>
    {
        QueryHandler<IEnumerable<Category>> _queryHandler;
        public CategoriesController(CommnadHandler commnadHandler,
            IJsonStore<Category> store,
            QueryHandler<IEnumerable<Category>> queryHandler) : base(commnadHandler, store)
        {
            _queryHandler = queryHandler;
        }
        public async Task<QueryResult<IEnumerable<Category>>> GetCategories(Guid? parentId)
        {
            return await _queryHandler.HandleAsync<Guid?>(new CategoriesByParentQuery(_store), parentId);
        }
    }
}
