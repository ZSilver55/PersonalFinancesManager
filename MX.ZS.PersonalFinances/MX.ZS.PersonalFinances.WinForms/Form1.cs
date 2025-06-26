using MX.ZS.PersonalFinances.BLL;
using MX.ZS.PersonalFinances.Domain.Entities;

namespace MX.ZS.PersonalFinances.WinForms
{
    public partial class Form1 : Form
    {
        IProfileManager _profileManager;
        List<Account> _accounts;
        Account _currentAccount;
        public Form1(IProfileManager profileManager)
        {
            _profileManager = profileManager;
            InitializeComponent();
            UpdateAccounts();
        }
        void UpdateDeleteButtonState()
        {
            button3.Enabled = _accounts != null && _accounts.Any();
        }
        void UpdateCurrentSelectedAccount()
        {
            label1.Text = _currentAccount != null ? _currentAccount.Name : "";
        }
        void UpdateAccounts()
        {
            _accounts = _profileManager.GetAccounts();
            UpdateDeleteButtonState();
            //Does no exist, must be cleared from the current selection
            if (_currentAccount != null && !_accounts.Any(x => x.Id == _currentAccount.Id))
            {
                _currentAccount = null;
            }
            //Theres no selection made, select a default item
            if (_accounts.Any() && _accounts.Count >= 1 && _currentAccount == null)
            {
                _currentAccount = _accounts.First();
            }
            UpdateCurrentSelectedAccount();
            
        }
        private void button1_Click(object sender, EventArgs e)
        {
            _profileManager.AddAccount(new Account()
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
            });
            UpdateAccounts();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateAccounts();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(_currentAccount != null)
                _profileManager.RemoveAccount(_currentAccount);
            UpdateAccounts();
        }
    }
}
