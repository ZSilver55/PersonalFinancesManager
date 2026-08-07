using BudgetManager.Domain.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Domain
{
    public class Account : Aggregate
    {
        public Guid ProfileId { get; set; }
        [Required]
        public string Name { get; set; }

        public AccountType Type { get; set; }

        public decimal InitialBalance { get; set; }

        public bool IsArchived { get; set; } = false;

        public string Currency { get; set; } = "MXN";

        /// <summary>
        /// Annual interest rate as a percentage (e.g. 3.5 = 3.5%/yr). Only meaningful for
        /// savings accounts; 0 means no interest.
        /// </summary>
        public decimal AnnualInterestRate { get; set; } = 0m;

        /// <summary>How often the interest is compounded/applied.</summary>
        public Frequency InterestFrequency { get; set; } = Frequency.Monthly;

        /// <summary>The next date interest is applied. Null when interest is not scheduled.</summary>
        public DateTime? NextInterestDate { get; set; }
    }
}
