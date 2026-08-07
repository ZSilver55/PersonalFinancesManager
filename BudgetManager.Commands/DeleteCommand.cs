using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Commands
{
    /// <summary>
    /// Generic delete command for any aggregate, keyed by id,
    /// e.g. DeleteCommand&lt;Account&gt; invoked with a Guid.
    /// </summary>
    public class DeleteCommand<T> : IDeleteCommand<Guid> where T : Aggregate
    {
        private readonly IEntityStore<T> _store;

        public DeleteCommand(IEntityStore<T> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommandResult> ExecuteAsync(Guid id)
        {
            if (id == Guid.Empty)
                return CommandResult.Failed($"Cannot delete a {typeof(T).Name} without an id.");

            bool removed = await _store.DeleteAsync(id);
            return removed
                ? CommandResult.OK()
                : CommandResult.Failed($"{typeof(T).Name} with id '{id}' was not found.");
        }
    }
}
