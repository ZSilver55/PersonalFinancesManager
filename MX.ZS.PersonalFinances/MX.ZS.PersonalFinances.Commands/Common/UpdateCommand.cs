using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public abstract class UpdateCommand<TData> : IUpdateCommand<TData>
    {
        public void Execute(ICommandRepository<TData> respository, TData data)
        {
            respository.Update(data);
        }
    }
}
