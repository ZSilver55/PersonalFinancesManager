using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.UI.Dialogs
{
    public class RecurringEditDialog : Form
    {
        private readonly RecurringTransaction _model;
        private readonly TextBox _name = new();
        private readonly ComboBox _account = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _destination = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _frequency;
        private readonly NumericUpDown _amount;
        private readonly DateTimePicker _next = new() { Format = DateTimePickerFormat.Short };
        private readonly CheckBox _hasEnd = new() { Text = Loc.T("Has end date"), AutoSize = true };
        private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Short };
        private readonly Label _signHint = new() { Text = Loc.T("positive = income, negative = expense"), AutoSize = false, Height = 18, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SystemColors.GrayText };
        private readonly CheckBox _enabled = new() { Text = Loc.T("Enabled"), AutoSize = true };

        public RecurringTransaction Result => _model;

        public RecurringEditDialog(RecurringTransaction model, IEnumerable<Account> accounts, IEnumerable<Category> categories)
        {
            _model = model;
            var (table, _) = DialogUi.Build(this, Loc.T(model.Id == Guid.Empty ? "New recurring transaction" : "Edit recurring transaction"), height: 540);

            _name.Text = model.Name ?? "";
            _frequency = DialogUi.EnumCombo(typeof(Frequency), model.Frequency);
            _amount = DialogUi.Money(model.Amount);
            _next.Value = model.NextExecution ?? DateTime.Today;
            _enabled.Checked = model.Enabled;

            _hasEnd.Checked = model.EndDate.HasValue;
            _end.Value = model.EndDate ?? DateTime.Today;
            _end.Enabled = _hasEnd.Checked;
            _hasEnd.CheckedChanged += (_, _) => _end.Enabled = _hasEnd.Checked;

            foreach (var a in accounts)
                _account.Items.Add(new DialogUi.Item { Value = a.Id, Text = a.Name });

            _destination.Items.Add(new DialogUi.Item { Value = null, Text = Loc.T("(none)") });
            foreach (var a in accounts)
                _destination.Items.Add(new DialogUi.Item { Value = a.Id, Text = a.Name });

            _category.Items.Add(new DialogUi.Item { Value = null, Text = Loc.T("(none)") });
            foreach (var c in categories)
                _category.Items.Add(new DialogUi.Item { Value = c.Id, Text = c.Name });

            _account.SelectedIndex = Math.Max(0, IndexOfValue(_account, model.AccountId));
            _destination.SelectedIndex = model.DestinationAccountId.HasValue
                ? Math.Max(0, IndexOfValue(_destination, model.DestinationAccountId.Value)) : 0;
            _category.SelectedIndex = model.CategoryId.HasValue
                ? Math.Max(0, IndexOfValue(_category, model.CategoryId.Value)) : 0;

            DialogUi.Row(table, Loc.T("Name"), _name);
            DialogUi.Row(table, Loc.T("Account"), _account);
            DialogUi.Row(table, Loc.T("To account"), _destination);
            DialogUi.Row(table, "", new Label { Text = Loc.T("Set a destination to make it a transfer to your own account."), AutoSize = true, ForeColor = SystemColors.GrayText });
            DialogUi.Row(table, Loc.T("Amount"), _amount);
            DialogUi.Row(table, "", _signHint);
            DialogUi.Row(table, Loc.T("Category"), _category);
            DialogUi.Row(table, Loc.T("Frequency"), _frequency);
            DialogUi.Row(table, Loc.T("Next execution"), _next);
            DialogUi.Row(table, "", _hasEnd);
            DialogUi.Row(table, Loc.T("End date"), _end);
            DialogUi.Row(table, "", _enabled);

            _destination.SelectedIndexChanged += (_, _) => UpdateTransferMode();
            UpdateTransferMode();

            FormClosing += OnClosing;
        }

        private void UpdateTransferMode()
        {
            bool isTransfer = (_destination.SelectedItem as DialogUi.Item)?.Value is Guid;

            // For transfers the sign is meaningless (amount is moved as-is), so blank the hint
            // (keeping its row height so the layout doesn't shift) and force a positive amount.
            _signHint.Text = isTransfer ? "" : Loc.T("positive = income, negative = expense");
            if (isTransfer)
            {
                if (_amount.Value < 0m) _amount.Value = Math.Abs(_amount.Value);
                _amount.Minimum = 0m;
            }
            else
            {
                _amount.Minimum = -1_000_000_000m;
            }
        }

        private static int IndexOfValue(ComboBox cbo, object value)
        {
            for (int i = 0; i < cbo.Items.Count; i++)
                if (cbo.Items[i] is DialogUi.Item it && Equals(it.Value, value)) return i;
            return 0;
        }

        private void OnClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            var accountId = (_account.SelectedItem as DialogUi.Item)?.Value as Guid?;

            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                Reject(e, Loc.T("Name is required.")); return;
            }
            if (accountId is null)
            {
                Reject(e, Loc.T("An account is required.")); return;
            }
            if (_amount.Value == 0)
            {
                Reject(e, Loc.T("Amount cannot be zero.")); return;
            }

            var destinationId = (_destination.SelectedItem as DialogUi.Item)?.Value as Guid?;
            if (destinationId is not null && destinationId == accountId)
            {
                Reject(e, Loc.T("Source and destination must differ.")); return;
            }

            if (_hasEnd.Checked && _end.Value.Date < _next.Value.Date)
            {
                Reject(e, Loc.T("End date cannot be before the next run.")); return;
            }

            _model.Name = _name.Text.Trim();
            _model.AccountId = accountId.Value;
            _model.DestinationAccountId = destinationId;
            _model.CategoryId = (_category.SelectedItem as DialogUi.Item)?.Value as Guid?;
            _model.Amount = _amount.Value;
            _model.Frequency = (Frequency)_frequency.SelectedItem!;
            _model.NextExecution = _next.Value.Date;
            _model.EndDate = _hasEnd.Checked ? _end.Value.Date : null;
            _model.Enabled = _enabled.Checked;
        }

        private void Reject(FormClosingEventArgs e, string message)
        {
            e.Cancel = true;
            MessageBox.Show(this, message, Loc.T("Validation"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
