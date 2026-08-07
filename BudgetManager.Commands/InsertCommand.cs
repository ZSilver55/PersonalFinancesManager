using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Commands
{
    /// <summary>
    /// Generic insert command for any aggregate, e.g. InsertCommand&lt;Account&gt;.
    /// A fresh id is assigned when the incoming item does not carry one.
    /// </summary>
    public class InsertCommand<T> : IInsertCommand<T> where T : Aggregate
    {
        private readonly IJsonStore<T> _store;

        public InsertCommand(IJsonStore<T> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommandResult> ExecuteAsync(T parameters)
        {
            if (parameters is null)
                return CommandResult.Failed($"Cannot insert a null {typeof(T).Name}.");

            if (parameters.Id == Guid.Empty)
                parameters.Id = Guid.NewGuid();

            var existing = await _store.FindAsync(parameters.Id);
            if (existing is not null)
                return CommandResult.Failed($"{typeof(T).Name} with id '{parameters.Id}' already exists.");

            await _store.UpsertAsync(parameters);
            return CommandResult.OK();
        }
    }
}
