using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetManager.Domain
{
    public abstract class Aggregate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
