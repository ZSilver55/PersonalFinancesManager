using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.DAL.Common
{
    public interface IQueryRepository<TResult, TFilter>
    {
        TResult Read(TFilter filter);
    }
}
