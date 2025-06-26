using MX.ZS.PersonalFinances.Domain.Entities;

namespace MX.ZS.PersonalFinances.BLL
{
    public interface IProfileManager
    {
        List<Account> GetAccounts();
        void AddAccount(Account entity);
        void RemoveAccount(Account entity);
        void UpdateAccount(Account entity);
    }
}
