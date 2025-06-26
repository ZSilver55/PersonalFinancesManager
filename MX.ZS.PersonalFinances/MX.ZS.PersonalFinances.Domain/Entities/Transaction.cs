using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.Domain.Entities
{
    public abstract class Transaction : Entity
    {
        public Guid? ParentTransactionId { get; set; }
        public Guid AccountId { get; set; }
        public string Name { get; set; }
        public decimal Ammount { get; set; }
        public DateTime? Date { get; set; }
        public bool Applied { get; set; }
        public string Comments { get; set; }
    }
}
