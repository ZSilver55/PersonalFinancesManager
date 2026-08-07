namespace BudgetManager.Queries.Common
{
    public interface IQueryAll<TResult>
    {
        Task<QueryResult<TResult>> ExecuteQueryAsync();
    }
    public interface IQuery<TResult, TParameter>
    {
        Task<QueryResult<TResult>> ExecuteQueryAsync(TParameter parameter);
    }
}
