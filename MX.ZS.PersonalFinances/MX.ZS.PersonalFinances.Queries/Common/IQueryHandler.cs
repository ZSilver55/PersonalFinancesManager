namespace MX.ZS.PersonalFinances.Queries.Common
{
    public interface IQueryHandler
    {
        TResult Handle<TResult, TFilter>(IQuery<TResult, TFilter> query, TFilter data);
    }
}
