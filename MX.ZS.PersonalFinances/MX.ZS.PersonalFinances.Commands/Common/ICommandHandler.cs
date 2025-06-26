namespace MX.ZS.PersonalFinances.Commands.Common
{
    public interface ICommandHandler
    {
        void Handle<TResult>(ICommand<TResult> command, TResult data);
    }
}
