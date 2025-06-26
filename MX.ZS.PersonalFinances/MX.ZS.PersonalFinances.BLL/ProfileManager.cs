using Microsoft.Extensions.Configuration;
using MX.ZS.PersonalFinances.Commands;
using MX.ZS.PersonalFinances.Commands.Common;
using MX.ZS.PersonalFinances.Domain.DTO.Request;
using MX.ZS.PersonalFinances.Domain.Entities;
using MX.ZS.PersonalFinances.Queries;
using MX.ZS.PersonalFinances.Queries.Common;

namespace MX.ZS.PersonalFinances.BLL
{
    public class ProfileManager : IProfileManager
    {
        IQueryHandler _queryHandler;
        ICommandHandler _commandHandler;
        IConfiguration _configuration;
        public ProfileManager(IConfiguration configuration, ICommandHandler commandHandler, IQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
            _commandHandler = commandHandler;
            _configuration = configuration;
        }
        public void AddAccount(Account entity)
        {
            _commandHandler.Handle(new InsertAccountCommand(), entity);
        }

        public List<Account> GetAccounts()
        {
            return _queryHandler.Handle(new RequestAccountsQuery(), new RequestAccounts());
        }

        public void RemoveAccount(Account entity)
        {
            _commandHandler.Handle(new DeleteAccountCommand(), entity);
        }

        public void UpdateAccount(Account entity)
        {
            _commandHandler.Handle(new UpdateAccountCommand(), entity);
        }
    }
}
