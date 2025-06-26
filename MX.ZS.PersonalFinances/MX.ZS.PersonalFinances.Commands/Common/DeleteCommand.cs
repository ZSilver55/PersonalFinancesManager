using System.Data.Common;
using MX.ZS.PersonalFinances.DAL.Common;
using MX.ZS.PersonalFinances.Domain.Entities;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public class DeleteCommand<TData> : IDeleteCommand<TData> where TData : Entity
    {
        public void Execute(ICommandRepository<TData> respository, TData data)
        {
            respository.Delete(data.Id);
        }
    }
}
