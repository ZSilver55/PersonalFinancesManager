using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public abstract class InsertCommand<TData> : IInsertCommand<TData>
    {
        public void Execute(ICommandRepository<TData> respository, TData data)
        {
            respository.Create(data);
        }
    }
}
