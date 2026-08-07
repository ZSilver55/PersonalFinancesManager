namespace BudgetManager.Queries.Common
{
    public class QueryResult<TResult>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TResult Data { get; set; }
        public static QueryResult<TResult> OK(TResult data) => new QueryResult<TResult> { Success = true, Data = data };
        public static QueryResult<TResult> Failed(string message) => new QueryResult<TResult>() { Success = false, Message = message };
        public static QueryResult<TResult> Failed(Exception ex) => new QueryResult<TResult>() { Success = false, Message = ex.Message };
        public static QueryResult<TResult> Failed(ArgumentException ex) => Failed(ex.Message);
        public static QueryResult<TResult> Failed(NullReferenceException ex) => Failed(ex.Message);
        public static QueryResult<TResult> Failed(ArgumentNullException ex) => Failed(ex.Message);
    }
}
