namespace BudgetManager.Domain
{
    public class Attachment : Aggregate
    {
        public Guid TransactionId { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public byte[] Data { get; set; }
    }
}
