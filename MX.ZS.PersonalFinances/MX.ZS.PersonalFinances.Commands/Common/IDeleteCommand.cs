using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public interface IDeleteCommand<TData> : ICommand<TData>
    {
        void Execute(ICommandRepository<TData> respository, TData entity);
    }
}
