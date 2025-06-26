using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.DAL.Common
{
    public interface ICommandRepository<TResult>
    {
        void Create(TResult entity);
        void Update(TResult entity);
        void Delete(Guid id);
    }
}
