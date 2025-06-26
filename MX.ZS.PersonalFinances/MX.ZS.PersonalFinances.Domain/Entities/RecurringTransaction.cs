using MX.ZS.PersonalFinances.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Entities
{
    public abstract class RecurringTransaction : Transaction
    {
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ePeriodicity Periodicity { get; set; }
    }
}
