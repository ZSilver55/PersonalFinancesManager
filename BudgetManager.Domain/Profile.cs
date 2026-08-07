using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetManager.Domain
{
    public class Profile : Aggregate
    {
        [Required]
        public string Names { get; set; }
        [Required]
        public string LastNames { get; set; }
        [EmailAddress]
        public string Email { get; set; }
    }
}
