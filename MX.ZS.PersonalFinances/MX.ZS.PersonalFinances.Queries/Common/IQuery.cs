using MX.ZS.PersonalFinances.DAL.Common;

namespace MX.ZS.PersonalFinances.Queries.Common
{
    public interface IQuery<TResult, TFilter>
    {
        TResult Execute(IQueryRepository<TResult, TFilter> repository, TFilter data);
    }
}
