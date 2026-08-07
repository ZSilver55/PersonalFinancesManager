namespace BudgetManager.Commands
{
    public interface IInsertCommand<TParameters> : ICommand<TParameters> { }
    public interface IUpdateCommand<TParameters> : ICommand<TParameters> { }
    public interface IDeleteCommand<TParameters> : ICommand<TParameters> { }
}
