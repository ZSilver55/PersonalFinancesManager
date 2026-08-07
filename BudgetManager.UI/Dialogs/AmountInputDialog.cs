namespace BudgetManager.UI.Dialogs
{
    /// <summary>Minimal dialog to capture a single amount.</summary>
    public class AmountInputDialog : Form
    {
        private readonly NumericUpDown _amount;

        public decimal Amount => _amount.Value;

        public AmountInputDialog(string title, string label, decimal initial = 0m)
        {
            var (table, _) = DialogUi.Build(this, title, height: 170);
            _amount = DialogUi.Money(initial);
            DialogUi.Row(table, label, _amount);
        }
    }
}
