using Microsoft.Extensions.Logging;

namespace BudgetManager.Queries.Common
{
    public class QueryHandler<TResult>
    {
        ILogger<QueryHandler<TResult>> _logger;
        public QueryHandler(ILogger<QueryHandler<TResult>> logger)
        {
            _logger = logger;
        }
        public async Task<QueryResult<TResult>> HandleAsync(IQueryAll<TResult> query)
        {
            try
            {
                return await query.ExecuteQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return QueryResult<TResult>.Failed(ex);
            }
        }
        public async Task<QueryResult<TResult>> HandleAsync<TParameters>(IQuery<TResult, TParameters> query, TParameters parameters)
        {
            try
            {
                return await query.ExecuteQueryAsync(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return QueryResult<TResult>.Failed(ex);
            }
        }
    }
}
