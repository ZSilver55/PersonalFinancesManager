namespace BudgetManager.UI.Dialogs
{
    /// <summary>Edits the "safe to spend" settings: safety buffer and the goals reserve toggle.</summary>
    public class SafeToSpendConfigDialog : Form
    {
        private readonly NumericUpDown _buffer;
        private readonly CheckBox _reserveGoals = new() { Text = Loc.T("Reserve for goals"), AutoSize = true };

        public decimal Buffer => _buffer.Value;
        public bool ReserveGoals => _reserveGoals.Checked;

        public SafeToSpendConfigDialog(decimal buffer, bool reserveGoals)
        {
            var (table, _) = DialogUi.Build(this, Loc.T("Safe-to-spend settings"), height: 220);

            _buffer = DialogUi.Money(buffer, min: 0m);
            _reserveGoals.Checked = reserveGoals;

            DialogUi.Row(table, Loc.T("Safety buffer"), _buffer);
            DialogUi.Row(table, "", _reserveGoals);
        }
    }
}
