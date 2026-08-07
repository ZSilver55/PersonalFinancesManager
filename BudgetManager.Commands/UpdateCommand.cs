using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Commands
{
    /// <summary>
    /// Generic update command for any aggregate, e.g. UpdateCommand&lt;Account&gt;.
    /// Fails when the target item does not already exist.
    /// </summary>
    public class UpdateCommand<T> : IUpdateCommand<T> where T : Aggregate
    {
        private readonly IEntityStore<T> _store;

        public UpdateCommand(IEntityStore<T> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommandResult> ExecuteAsync(T parameters)
        {
            if (parameters is null)
                return CommandResult.Failed($"Cannot update a null {typeof(T).Name}.");

            if (parameters.Id == Guid.Empty)
                return CommandResult.Failed($"Cannot update a {typeof(T).Name} without an id.");

            var existing = await _store.FindAsync(parameters.Id);
            if (existing is null)
                return CommandResult.Failed($"{typeof(T).Name} with id '{parameters.Id}' was not found.");

            await _store.UpsertAsync(parameters);
            return CommandResult.OK();
        }
    }
}
