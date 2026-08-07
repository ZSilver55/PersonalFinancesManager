using BudgetManager.Domain;

namespace BudgetManager.UI.Dialogs
{
    public class GoalEditDialog : Form
    {
        private readonly Goal _goal;
        private readonly TextBox _name = new();
        private readonly NumericUpDown _target;
        private readonly NumericUpDown _current;
        private readonly CheckBox _hasDue = new() { Text = Loc.T("Has due date"), AutoSize = true };
        private readonly DateTimePicker _due = new() { Format = DateTimePickerFormat.Short };

        public Goal Result => _goal;

        public GoalEditDialog(Goal goal)
        {
            _goal = goal;
            var (table, _) = DialogUi.Build(this, Loc.T(goal.Id == Guid.Empty ? "New goal" : "Edit goal"), height: 300);

            _name.Text = goal.Name ?? "";
            _target = DialogUi.Money(goal.TargetAmount, min: 0m);
            _current = DialogUi.Money(goal.CurrentAmount, min: 0m);

            _hasDue.Checked = goal.DueDate.HasValue;
            _due.Value = goal.DueDate ?? DateTime.Today;
            _due.Enabled = _hasDue.Checked;
            _hasDue.CheckedChanged += (_, _) => _due.Enabled = _hasDue.Checked;

            DialogUi.Row(table, Loc.T("Name"), _name);
            DialogUi.Row(table, Loc.T("Target amount"), _target);
            DialogUi.Row(table, Loc.T("Current amount"), _current);
            DialogUi.Row(table, "", _hasDue);
            DialogUi.Row(table, Loc.T("Due date"), _due);

            FormClosing += OnClosing;
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

            _goal.Name = _name.Text.Trim();
            _goal.TargetAmount = _target.Value;
            _goal.CurrentAmount = _current.Value;
            _goal.DueDate = _hasDue.Checked ? _due.Value.Date : null;
        }
    }
}
