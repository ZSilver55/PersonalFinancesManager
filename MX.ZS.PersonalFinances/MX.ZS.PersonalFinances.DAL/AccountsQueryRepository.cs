using Microsoft.Extensions.Configuration;
using MX.ZS.PersonalFinances.DAL.Common;
using MX.ZS.PersonalFinances.Domain.DTO.Request;
using MX.ZS.PersonalFinances.Domain.Entities;

namespace MX.ZS.PersonalFinances.DAL
{
    public class AccountsQueryRepository : FindAllLiteDBQueryRepository<Account, RequestAccounts>
    {
        public AccountsQueryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected override IConfiguration _configuration { get; }
    }
}
