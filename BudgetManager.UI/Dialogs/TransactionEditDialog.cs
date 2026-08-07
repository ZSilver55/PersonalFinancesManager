using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.UI.Dialogs
{
    public class TransactionEditDialog : Form
    {
        private readonly Transaction _transaction;
        private readonly ComboBox _type;
        private readonly ComboBox _source = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _destination = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _amount;
        private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short };
        private readonly TextBox _description = new();

        public Transaction Result => _transaction;

        public TransactionEditDialog(Transaction transaction, IEnumerable<Account> accounts, IEnumerable<Category> categories)
        {
            _transaction = transaction;
            var (table, _) = DialogUi.Build(this, Loc.T(transaction.Id == Guid.Empty ? "New transaction" : "Edit transaction"), height: 380);

            _type = DialogUi.EnumCombo(typeof(TransactionType), transaction.Type);
            _amount = DialogUi.Money(transaction.Amount, min: 0m);
            _date.Value = transaction.Date == default ? DateTime.Today : transaction.Date;
            _description.Text = transaction.Description ?? "";

            foreach (var a in accounts)
            {
                var item = new DialogUi.Item { Value = a.Id, Text = a.Name };
                _source.Items.Add(item);
                _destination.Items.Add(new DialogUi.Item { Value = a.Id, Text = a.Name });
            }
            _destination.Items.Insert(0, new DialogUi.Item { Value = null, Text = Loc.T("(none)") });

            _category.Items.Add(new DialogUi.Item { Value = null, Text = Loc.T("(none)") });
            foreach (var c in categories)
                _category.Items.Add(new DialogUi.Item { Value = c.Id, Text = c.Name });

            _source.SelectedIndex = Math.Max(0, IndexOfValue(_source, transaction.SourceAccountId));
            _destination.SelectedIndex = transaction.DestinationAccountId.HasValue
                ? Math.Max(0, IndexOfValue(_destination, transaction.DestinationAccountId.Value)) : 0;
            _category.SelectedIndex = transaction.CategoryId.HasValue
                ? Math.Max(0, IndexOfValue(_category, transaction.CategoryId.Value)) : 0;

            DialogUi.Row(table, Loc.T("Type"), _type);
            DialogUi.Row(table, Loc.T("From account"), _source);
            DialogUi.Row(table, Loc.T("To account"), _destination);
            DialogUi.Row(table, Loc.T("Category"), _category);
            DialogUi.Row(table, Loc.T("Amount"), _amount);
            DialogUi.Row(table, Loc.T("Date"), _date);
            DialogUi.Row(table, Loc.T("Description"), _description);

            _type.SelectedIndexChanged += (_, _) => UpdateTransferState();
            UpdateTransferState();

            FormClosing += OnClosing;
        }

        private bool IsTransfer => (TransactionType)_type.SelectedItem! == TransactionType.Transfer;

        private void UpdateTransferState()
        {
            _destination.Enabled = IsTransfer;
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

            var sourceId = (_source.SelectedItem as DialogUi.Item)?.Value as Guid?;
            var destId = (_destination.SelectedItem as DialogUi.Item)?.Value as Guid?;
            var categoryId = (_category.SelectedItem as DialogUi.Item)?.Value as Guid?;

            if (sourceId is null)
            {
                Reject(e, Loc.T("A source account is required.")); return;
            }
            if (_amount.Value <= 0)
            {
                Reject(e, Loc.T("Amount must be greater than zero.")); return;
            }
            if (IsTransfer)
            {
                if (destId is null) { Reject(e, Loc.T("A transfer needs a destination account.")); return; }
                if (destId == sourceId) { Reject(e, Loc.T("Source and destination must differ.")); return; }
            }

            _transaction.Type = (TransactionType)_type.SelectedItem!;
            _transaction.SourceAccountId = sourceId.Value;
            _transaction.DestinationAccountId = IsTransfer ? destId : null;
            _transaction.CategoryId = categoryId;
            _transaction.Amount = _amount.Value;
            _transaction.Date = _date.Value;
            _transaction.Description = _description.Text.Trim();
        }

        private void Reject(FormClosingEventArgs e, string message)
        {
            e.Cancel = true;
            MessageBox.Show(this, message, Loc.T("Validation"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
