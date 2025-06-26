using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public class CommandHandler : ICommandHandler
    {
        IRepositoryFactory _repositoryFactory;
        public CommandHandler(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
        }

        public void Handle<TResult>(ICommand<TResult> command, TResult data)
        {
            command.Execute(_repositoryFactory.FabricateCommandRespository<TResult>(), data);
        }
    }
}
