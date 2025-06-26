using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Enums
{
    public enum ePeriodicity
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        BiWeekly = 3,
        Monthly = 4,
        Bimonthly = 5,
        Quarterly = 6,
        FourMonthly = 7,
        SemiAnnual = 8,
        Annual = 9,
        Irregular = 12,
        FixedMonthDay = 13
    }
}
