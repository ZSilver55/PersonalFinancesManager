using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Attachments
{
    /// <summary>
    /// Returns all attachments linked to a given transaction.
    /// </summary>
    public class AttachmentsByTransactionQuery : JsonQuery<Attachment>, IQuery<IEnumerable<Attachment>, Guid>
    {
        public AttachmentsByTransactionQuery(IEntityStore<Attachment> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<Attachment>>> ExecuteQueryAsync(Guid transactionId)
        {
            var matches = new List<Attachment>();
            await foreach (var a in Store.StreamAllAsync())
            {
                if (a.TransactionId == transactionId)
                    matches.Add(a);
            }
            return QueryResult<IEnumerable<Attachment>>.OK(matches);
        }
    }
}
