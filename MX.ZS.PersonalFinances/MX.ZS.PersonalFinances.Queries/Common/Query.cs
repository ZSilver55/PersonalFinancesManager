using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Queries.Common
{
    public abstract class Query<TResult, TFilter> : IQuery<TResult, TFilter>
    {
        public TResult Execute(IQueryRepository<TResult, TFilter> repository, TFilter data)
        {
            return repository.Read(data);
        }
    }
}
