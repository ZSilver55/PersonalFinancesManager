using BudgetManager.Domain.Enumerations;

namespace BudgetManager.Domain
{
    public class Transaction : Aggregate
    {
        public Guid SourceAccountId { get; set; }
        public Guid? DestinationAccountId { get; set; }
        public Guid? CategoryId { get; set; }
        public IEnumerable<Tag>? Tags { get; set; } = Enumerable.Empty<Tag>();
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}
