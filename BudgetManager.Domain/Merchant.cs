using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetManager.Domain
{
    public class Merchant : Aggregate
    {
        [Required]
        public string Name { get; set; }

        public string Notes { get; set; }
    }
}
