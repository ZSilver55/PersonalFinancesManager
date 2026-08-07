using BudgetManager.Domain;

namespace BudgetManager.UI.Dialogs
{
    public class ProfileEditDialog : Form
    {
        private readonly Profile _profile;
        private readonly TextBox _names = new();
        private readonly TextBox _lastNames = new();
        private readonly TextBox _email = new();

        public Profile Result => _profile;

        public ProfileEditDialog(Profile profile)
        {
            _profile = profile;
            var (table, _) = DialogUi.Build(this, Loc.T(profile.Id == Guid.Empty ? "New profile" : "Edit profile"), height: 240);

            _names.Text = profile.Names ?? "";
            _lastNames.Text = profile.LastNames ?? "";
            _email.Text = profile.Email ?? "";

            DialogUi.Row(table, Loc.T("First names"), _names);
            DialogUi.Row(table, Loc.T("Last names"), _lastNames);
            DialogUi.Row(table, Loc.T("Email"), _email);

            FormClosing += OnClosing;
        }

        private void OnClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(_names.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this, Loc.T("First names are required."), Loc.T("Validation"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _profile.Names = _names.Text.Trim();
            _profile.LastNames = _lastNames.Text.Trim();
            _profile.Email = _email.Text.Trim();
        }
    }
}
