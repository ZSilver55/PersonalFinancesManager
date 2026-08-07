using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Attachments;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    public class AttachementsController : BaseController<Attachment>
    {
        QueryHandler<IEnumerable<Attachment>> _queryHandler;
        public AttachementsController(CommnadHandler commnadHandler,
            IJsonStore<Attachment> store,
            QueryHandler<IEnumerable<Attachment>> queryHandler) : base(commnadHandler, store)
        {
            _queryHandler = queryHandler;
        }
        public async Task<QueryResult<IEnumerable<Attachment>>> GetAttachment(Guid parentId)
        {
            return await _queryHandler.HandleAsync<Guid>(new AttachmentsByTransactionQuery(_store), parentId);
        }
    }
}
