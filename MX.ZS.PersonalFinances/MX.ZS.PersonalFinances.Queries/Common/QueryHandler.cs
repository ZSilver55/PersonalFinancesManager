using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Queries.Common
{
    public class QueryHandler : IQueryHandler
    {
        IRepositoryFactory _repositoryFactory;
        public QueryHandler(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
        }
        public TResult Handle<TResult, TFilter>(IQuery<TResult, TFilter> query, TFilter data)
        {
            return query.Execute(_repositoryFactory.FabricateQueryRepository<TResult, TFilter>(), data);
        }
    }
}
