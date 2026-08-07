using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.UI.Dialogs
{
    public class AccountEditDialog : Form
    {
        private readonly Account _account;
        private readonly TextBox _name = new();
        private readonly ComboBox _type;
        private readonly NumericUpDown _initial;
        private readonly TextBox _currency = new();
        private readonly CheckBox _archived = new() { Text = Loc.T("Archived"), AutoSize = true };

        private readonly CheckBox _earnsInterest = new() { Text = Loc.T("Earns interest"), AutoSize = true };
        private readonly NumericUpDown _rate = new() { DecimalPlaces = 2, Minimum = 0m, Maximum = 100m, Increment = 0.25m };
        private readonly ComboBox _interestFreq = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker _nextInterest = new() { Format = DateTimePickerFormat.Short };

        public Account Result => _account;

        public AccountEditDialog(Account account)
        {
            _account = account;
            var (table, _) = DialogUi.Build(this, Loc.T(account.Id == Guid.Empty ? "New account" : "Edit account"), height: 440);

            _name.Text = account.Name ?? "";
            _type = DialogUi.EnumCombo(typeof(AccountType), account.Type);
            _initial = DialogUi.Money(account.InitialBalance);
            _currency.Text = string.IsNullOrWhiteSpace(account.Currency) ? "MXN" : account.Currency;
            _archived.Checked = account.IsArchived;

            // Interest (savings accounts only).
            foreach (var f in new[] { Frequency.Daily, Frequency.Weekly, Frequency.Monthly, Frequency.Quarterly, Frequency.Biannual, Frequency.Yearly })
                _interestFreq.Items.Add(f);
            bool periodic = account.InterestFrequency is Frequency.Daily or Frequency.Weekly or Frequency.Monthly
                            or Frequency.Quarterly or Frequency.Biannual or Frequency.Yearly;
            _interestFreq.SelectedItem = periodic ? account.InterestFrequency : Frequency.Monthly;
            _rate.Value = Math.Clamp(account.AnnualInterestRate, 0m, 100m);
            _nextInterest.Value = account.NextInterestDate ?? DateTime.Today;
            _earnsInterest.Checked = account.AnnualInterestRate > 0m;

            DialogUi.Row(table, Loc.T("Name"), _name);
            DialogUi.Row(table, Loc.T("Type"), _type);
            DialogUi.Row(table, Loc.T("Initial balance"), _initial);
            DialogUi.Row(table, Loc.T("Currency"), _currency);
            DialogUi.Row(table, "", _earnsInterest);
            DialogUi.Row(table, Loc.T("Annual rate %"), _rate);
            DialogUi.Row(table, Loc.T("Interest frequency"), _interestFreq);
            DialogUi.Row(table, Loc.T("Next interest date"), _nextInterest);
            DialogUi.Row(table, "", _archived);

            _type.SelectedIndexChanged += (_, _) => UpdateInterestState();
            _earnsInterest.CheckedChanged += (_, _) => UpdateInterestState();
            UpdateInterestState();

            FormClosing += OnClosing;
        }

        private void UpdateInterestState()
        {
            bool savings = (AccountType)_type.SelectedItem! == AccountType.Savings;
            _earnsInterest.Enabled = savings;
            if (!savings) _earnsInterest.Checked = false;

            bool on = savings && _earnsInterest.Checked;
            _rate.Enabled = on;
            _interestFreq.Enabled = on;
            _nextInterest.Enabled = on;
        }

        private void OnClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this, Loc.T("Name is required."), Loc.T("Validation"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _account.Name = _name.Text.Trim();
            _account.Type = (AccountType)_type.SelectedItem!;
            _account.InitialBalance = _initial.Value;
            _account.Currency = string.IsNullOrWhiteSpace(_currency.Text) ? "MXN" : _currency.Text.Trim();
            _account.IsArchived = _archived.Checked;

            bool savings = _account.Type == AccountType.Savings;
            if (savings && _earnsInterest.Checked)
            {
                _account.AnnualInterestRate = _rate.Value;
                _account.InterestFrequency = (Frequency)_interestFreq.SelectedItem!;
                _account.NextInterestDate = _nextInterest.Value.Date;
            }
            else
            {
                _account.AnnualInterestRate = 0m;
                _account.NextInterestDate = null;
            }
        }
    }
}
