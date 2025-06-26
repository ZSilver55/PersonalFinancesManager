using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Entities
{
    public class Account : Entity
    {
        public string Name { get; set; }
        public Account()
        {
            this.Transactions = new List<Transaction>();
        }
        public decimal Balance { get
            {
                if (Transactions != null && Transactions.Any(x => x.Applied))
                    return Transactions.Sum(x => x.Ammount);
                return 0m;
            }
        }
        public List<Transaction> Transactions { get; set; }
        public List<RecurringTransaction> ScheduledTransactions { get; set; }
    }
}
