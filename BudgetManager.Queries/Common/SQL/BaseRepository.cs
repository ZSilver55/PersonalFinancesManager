using BudgetManager.Domain;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetManager.Queries.Common.SQL
{
    public abstract class BaseRepository
    {
        #region Definitions

        protected readonly IDbConnectionFactory _connectionFactory;
        protected readonly string _connectionString;

        #endregion Definitions

        #region Constructor

        protected BaseRepository(
            IDbConnectionFactory connectionFactory,
            IOptions<Settings> settings)
        {
            _connectionFactory = connectionFactory;
            _connectionString = settings.Value.ConnectionString;
        }

        #endregion Constructor

        #region Protected

        protected IDbConnection OpenConnection()
        {
            IDbConnection connection = _connectionFactory.Create(_connectionString);
            connection.Open();
            return connection;
        }

        protected abstract string GetScript();

        #endregion Protected
    }
}
