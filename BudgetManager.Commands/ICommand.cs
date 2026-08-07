namespace BudgetManager.Commands
{
    public interface ICommand<TParameters>
    {
        Task<CommandResult> ExecuteAsync(TParameters parameters);
    }
}
