using MX.ZS.PersonalFinances.BLL;
using MX.ZS.PersonalFinances.Domain.Entities;
using System.ComponentModel;

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
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Id";
            UpdateInterface();
        }
        void UpdateAccountButtonsState()
        {
            button3.Enabled = _accounts != null && _accounts.Any();
            button1.Enabled = !string.IsNullOrEmpty(textBox1.Text);
        }
        void ClearAccountNameTextBox()
        {
            textBox1.Text = string.Empty;
        }
        void UpdateCurrentAccount()
        {
            var previousAccount = _currentAccount;
            //Does no exist, must be cleared from the current selection
            if (_currentAccount != null && !_accounts.Any(x => x.Id == _currentAccount.Id))
            {
                _currentAccount = null;
                comboBox1.Text = "";
            }
            //Theres no selection made, select a default item
            if (_accounts.Any() && _accounts.Count >= 1 && _currentAccount == null)
            {
                _currentAccount = _accounts.First();
            }
            if (_currentAccount != previousAccount)
                AccountChanged();
        }
        void AccountChanged()
        {
            if(_transactionForm != null && _transactionForm.Visible)
                _transactionForm.Close();
            _transactionForm = null;
        }
        void UpdateAccountsDataSource()
        {
            comboBox1.DataSource = _accounts;
        }
        void UpdateInterface(bool isFromComboBox = false, bool isFromTextBox = false)
        {
            #region Accounts
            if (!isFromTextBox)
                ClearAccountNameTextBox();
            if (!isFromComboBox)
                _accounts = _profileManager.GetAccounts();
            if(!isFromTextBox)
                UpdateAccountsDataSource();
            UpdateAccountButtonsState();
            UpdateCurrentAccount();
            #endregion
            #region Transactions
            UpdateTransactionsButtonsState();
            UpdateTransactionsGridView();
            #endregion
        }
        void UpdateTransactionsButtonsState()
        {
            button2.Enabled = button6.Enabled = _currentAccount != null;
        }
        void UpdateTransactionsGridView()
        {
            if (_currentAccount != null)
            {
                BindingList<Transaction> transactions = new BindingList<Transaction>(_currentAccount.Transactions);
                dataGridView1.DataSource = transactions;
            }
            else
                dataGridView1.DataSource = null;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            _profileManager.AddAccount(new Account()
            {
                Id = Guid.NewGuid(),
                Name = textBox1.Text,
            });
            UpdateInterface();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_currentAccount != null)
                _profileManager.RemoveAccount(_currentAccount);
            UpdateInterface();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Guid selectedValue = (Guid)((ComboBox)sender).SelectedValue;
            if (_accounts.Any() && _accounts.Count >= 1)
            {
                _currentAccount = _accounts.First(x => x.Id == selectedValue);
            }
            UpdateInterface(isFromComboBox: true);
        }

        private void textBox1_Interacted(object sender, EventArgs e)
        {
            UpdateInterface(isFromTextBox: true);
        }
        TransactionForm _transactionForm;
        void LoadTransactionForm(Transaction entity)
        {
            if (_transactionForm == null)
                _transactionForm = new TransactionForm(entity, ref _profileManager, ref _currentAccount, UpdateTransactionsGridView);
            else
            {
                _transactionForm.Close();
                _transactionForm = new TransactionForm(entity, ref _profileManager, ref _currentAccount, UpdateTransactionsGridView);
            }
            _transactionForm.Show();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            LoadTransactionForm(new Income()
            {
                Id = Guid.NewGuid(),
                AccountId = _currentAccount.Id,
            });
        }

        private void button6_Click(object sender, EventArgs e)
        {
            LoadTransactionForm(new Expense()
            {
                Id = Guid.NewGuid(),
                AccountId = _currentAccount.Id,
            });
        }
    }
}
