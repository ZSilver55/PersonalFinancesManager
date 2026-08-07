using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    public class BaseController<T> where T : Aggregate
    {
        CommnadHandler _commandHandler;
        protected IEntityStore<T> _store;
        public BaseController(CommnadHandler commnadHandler,
            IEntityStore<T> store)
        {
            _commandHandler = commnadHandler;
            _store = store;
        }

        public async Task<CommandResult> Add(T entity)
        {
            return await _commandHandler.HandleAsync(new InsertCommand<T>(_store), entity);
        }
        public async Task<CommandResult> Update(T entity)
        {
            return await _commandHandler.HandleAsync(new UpdateCommand<T>(_store), entity);
        }
        public async Task<CommandResult> Delete(Guid id)
        {
            return await _commandHandler.HandleAsync(new DeleteCommand<T>(_store), id);
        }

        /// <summary>Returns every stored entity of this type.</summary>
        public async Task<QueryResult<IEnumerable<T>>> GetAll()
        {
            var items = await _store.ReadAllAsync();
            return QueryResult<IEnumerable<T>>.OK(items);
        }

        /// <summary>Returns a single entity by id, or a failed result when not found.</summary>
        public async Task<QueryResult<T>> GetById(Guid id)
        {
            var item = await _store.FindAsync(id);
            return item is null
                ? QueryResult<T>.Failed($"{typeof(T).Name} with id '{id}' was not found.")
                : QueryResult<T>.OK(item);
        }
    }
}
