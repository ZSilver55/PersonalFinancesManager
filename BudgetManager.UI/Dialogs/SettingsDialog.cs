using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.UI.Dialogs
{
    /// <summary>Consolidated application settings: language, data source, and safe-to-spend.</summary>
    public class SettingsDialog : Form
    {
        private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox _online = new() { Text = Loc.T("Work online (use the server)"), AutoSize = true };
        private readonly TextBox _apiUrl = new();
        private readonly NumericUpDown _buffer;
        private readonly CheckBox _reserveGoals = new() { Text = Loc.T("Reserve for goals"), AutoSize = true };

        public string Language => (_language.SelectedItem as DialogUi.Item)?.Value as string ?? "en";
        public bool Online => _online.Checked;
        public string ApiBaseUrl => _apiUrl.Text.Trim();
        public decimal SafetyBuffer => _buffer.Value;
        public bool ReserveForGoals => _reserveGoals.Checked;

        public SettingsDialog(Settings current, bool currentOnline)
        {
            var (table, _) = DialogUi.Build(this, Loc.T("Settings"), width: 480, height: 360);

            _language.Items.Add(new DialogUi.Item { Value = "en", Text = "English" });
            _language.Items.Add(new DialogUi.Item { Value = "es", Text = "Español (MX)" });
            _language.SelectedIndex = string.Equals(current.Language, "es", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            _online.Checked = currentOnline;
            _apiUrl.Text = current.ApiBaseUrl ?? "";
            _buffer = DialogUi.Money(current.SafetyBuffer, min: 0m);
            _reserveGoals.Checked = current.ReserveForGoals;

            DialogUi.Row(table, Loc.T("Language"), _language);
            DialogUi.Row(table, "", _online);
            DialogUi.Row(table, Loc.T("API address"), _apiUrl);
            DialogUi.Row(table, "", new Label { Text = Loc.T("Switching online/offline migrates your data and restarts the app."), AutoSize = true, ForeColor = SystemColors.GrayText });
            DialogUi.Row(table, Loc.T("Safety buffer"), _buffer);
            DialogUi.Row(table, "", _reserveGoals);

            _online.CheckedChanged += (_, _) => _apiUrl.Enabled = _online.Checked;
            _apiUrl.Enabled = _online.Checked;

            FormClosing += OnClosing;
        }

        private void OnClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            if (_online.Checked && string.IsNullOrWhiteSpace(_apiUrl.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this, Loc.T("Enter the API address to work online."), Loc.T("Validation"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
