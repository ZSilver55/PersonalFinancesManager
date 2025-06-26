using LiteDB;
using Microsoft.Extensions.Configuration;
using MX.ZS.PersonalFinances.Domain.Entities;

namespace MX.ZS.PersonalFinances.DAL.Common
{
    public abstract class FindAllLiteDBQueryRepository<TResult, TFilter> : IQueryRepository<List<TResult>, TFilter>
    {
        protected abstract IConfiguration _configuration { get; }
        protected string TableName => nameof(TResult);
        public List<TResult> Read(TFilter filter)
        {
            List<TResult> result = new List<TResult>();
            using (var db = new LiteDatabase(_configuration["liteDBFilePath"]))
            {
                var collection = db.GetCollection<TResult>(TableName);
                result.AddRange(collection.FindAll());
            }
            return result;
        }
    }
}
