using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.DAL.Common
{
    public interface IRepositoryFactory
    {
        IQueryRepository<TResult, TFilter> FabricateQueryRepository<TResult, TFilter>();
        ICommandRepository<TResult> FabricateCommandRespository<TResult>();
    }
}
