using MX.ZS.PersonalFinances.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Entities
{
    public class RecurringIncome : RecurringTransaction
    {
        public eIncomeType IncomeType { get; set; }
    }
}
