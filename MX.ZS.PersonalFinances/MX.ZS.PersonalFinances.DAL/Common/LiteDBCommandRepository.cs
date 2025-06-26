using LiteDB;
using Microsoft.Extensions.Configuration;
using MX.ZS.PersonalFinances.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.DAL.Common
{
    public abstract class LiteDBCommandRepository<TResult> : ICommandRepository<TResult> where TResult : Entity
    {
        protected abstract IConfiguration _configuration { get; }
        protected string TableName => nameof(TResult);
        public void Create(TResult entity)
        {
            using (var db = new LiteDatabase(_configuration["liteDBFilePath"]))
            {
                var collection = db.GetCollection<TResult>(TableName);
                collection.Insert(entity);
            }
        }

        public void Delete(Guid id)
        {
            using (var db = new LiteDatabase(_configuration["liteDBFilePath"]))
            {
                var collection = db.GetCollection<TResult>(TableName);
                collection.DeleteMany(x => x.Id == id);
            }
        }

        public void Update(TResult entity)
        {
            using (var db = new LiteDatabase(_configuration["liteDBFilePath"]))
            {
                var collection = db.GetCollection<TResult>(TableName);
                collection.Update(entity);
            }
        }
    }
}
