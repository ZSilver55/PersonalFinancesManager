using BudgetManager.BLL;
using BudgetManager.Domain;
using BudgetManager.UI.Dialogs;

namespace BudgetManager.UI.Views
{
    public class AccountsView : UserControl, IRefreshableView
    {
        private readonly AccountController _controller;
        private readonly DataGridView _grid = Ui.Grid();
        private Guid _profileId;

        public AccountsView(AccountController controller)
        {
            _controller = controller;
            BuildUi();
        }

        private void BuildUi()
        {
            _grid.Columns.AddRange(
                Ui.Col(nameof(Account.Name), Loc.T("Name"), fill: 2f),
                Ui.Col(nameof(Account.Type), Loc.T("Type")),
                Ui.Col(nameof(Account.InitialBalance), Loc.T("Initial balance"), "N2"),
                Ui.Col(nameof(Account.Currency), Loc.T("Currency")),
                Ui.Col(nameof(Account.IsArchived), Loc.T("Archived")));

            var toolbar = Ui.Toolbar(
                Ui.Button(Loc.T("Add"), async (_, _) => await AddAsync()),
                Ui.Button(Loc.T("Edit"), async (_, _) => await EditAsync()),
                Ui.Button(Loc.T("Delete"), async (_, _) => await DeleteAsync()));

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        public async Task LoadAsync(Guid profileId)
        {
            _profileId = profileId;
            var result = await _controller.GetAccounts(profileId);
            _grid.DataSource = (result.Data ?? Enumerable.Empty<Account>()).ToList();
        }

        private Account? Selected => _grid.CurrentRow?.DataBoundItem as Account;

        private async Task AddAsync()
        {
            var account = new Account { ProfileId = _profileId, Currency = "MXN" };
            using var dlg = new AccountEditDialog(account);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var r = await _controller.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(_profileId);
        }

        private async Task EditAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select an account first.")); return; }
            using var dlg = new AccountEditDialog(Clone(Selected));
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var r = await _controller.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(_profileId);
        }

        private async Task DeleteAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select an account first.")); return; }
            if (MessageBox.Show(this, Loc.F("Delete account '{0}'?", Selected.Name), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = await _controller.Delete(Selected.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(_profileId);
        }

        private static Account Clone(Account a) => new()
        {
            Id = a.Id,
            ProfileId = a.ProfileId,
            Name = a.Name,
            Type = a.Type,
            InitialBalance = a.InitialBalance,
            IsArchived = a.IsArchived,
            Currency = a.Currency,
            AnnualInterestRate = a.AnnualInterestRate,
            InterestFrequency = a.InterestFrequency,
            NextInterestDate = a.NextInterestDate
        };

        private void Warn(string message) =>
            MessageBox.Show(this, message, Loc.T("Budget Manager"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
