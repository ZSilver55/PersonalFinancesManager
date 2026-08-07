using BudgetManager.Domain.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Domain
{
    public class RecurringTransaction : Aggregate
    {
        public Guid AccountId { get; set; }
        [Required]
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public Guid? CategoryId { get; set; }
        public Frequency Frequency { get; set; }
        public DateTime? NextExecution { get; set; }
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Optional last date the schedule runs (inclusive). Occurrences on or before this date
        /// are applied; once the next occurrence would fall after it, the item is disabled. Null
        /// means it runs indefinitely.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// When set, this recurring item is a transfer that moves the (absolute) amount from
        /// <see cref="AccountId"/> to this destination account. When null, it is an income/expense
        /// determined by the sign of <see cref="Amount"/>.
        /// </summary>
        public Guid? DestinationAccountId { get; set; }
    }
}
