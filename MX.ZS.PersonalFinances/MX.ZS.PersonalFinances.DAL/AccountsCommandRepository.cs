using LiteDB;
using Microsoft.Extensions.Configuration;
using MX.ZS.PersonalFinances.DAL.Common;
using MX.ZS.PersonalFinances.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MX.ZS.PersonalFinances.DAL
{
    public class AccountsCommandRepository : LiteDBCommandRepository<Account>
    {
        public AccountsCommandRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override IConfiguration _configuration { get; }
    }
}
