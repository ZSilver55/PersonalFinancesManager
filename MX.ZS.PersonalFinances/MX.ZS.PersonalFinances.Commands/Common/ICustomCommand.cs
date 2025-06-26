using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Commands.Common
{
    public interface ICustomCommand<TResult> : ICommand<TResult>
    {
        TResult Data { get; }
    }
}
