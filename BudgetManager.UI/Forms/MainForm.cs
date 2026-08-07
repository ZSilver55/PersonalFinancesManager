using System.Diagnostics;
using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Domain;
using BudgetManager.UI.Views;

namespace BudgetManager.UI.Forms
{
    /// <summary>
    /// Shell window: profile + language selectors, data tools (open folder / export / import),
    /// and a tab per screen. Changing the language rebuilds the tabs so every view/dialog is
    /// recreated in the chosen language.
    /// </summary>
    public class MainForm : Form
    {
        private readonly ProfileController _profiles;
        private readonly AccountController _accountsCtl;
        private readonly TransactionsController _transactionsCtl;
        private readonly CategoriesController _categoriesCtl;
        private readonly GoalController _goalsCtl;
        private readonly RecurringTransactionsController _recurringCtl;
        private readonly RecurringExecutionService _runner;
        private readonly InterestExecutionService _interest;
        private readonly BudgetService _budget;
        private readonly ProjectionService _projection;
        private readonly SafeToSpendService _safeToSpend;
        private readonly DataPortabilityService _data;
        private readonly AppSettingsService _appSettings;

        private readonly ComboBox _cboProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly ComboBox _cboLang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

        private Label _lblProfile = null!, _lblLang = null!;
        private Button _btnManage = null!, _btnOpen = null!, _btnExport = null!, _btnImport = null!, _btnRefresh = null!;
        private bool _suppressLang;
        private bool _suppressTabEvent;

        public MainForm(
            ProfileController profiles,
            AccountController accounts,
            TransactionsController transactions,
            CategoriesController categories,
            GoalController goals,
            RecurringTransactionsController recurring,
            RecurringExecutionService runner,
            InterestExecutionService interest,
            BudgetService budgetService,
            ProjectionService projection,
            SafeToSpendService safeToSpend,
            DataPortabilityService data,
            AppSettingsService appSettings)
        {
            _profiles = profiles;
            _accountsCtl = accounts;
            _transactionsCtl = transactions;
            _categoriesCtl = categories;
            _goalsCtl = goals;
            _recurringCtl = recurring;
            _runner = runner;
            _interest = interest;
            _budget = budgetService;
            _projection = projection;
            _safeToSpend = safeToSpend;
            _data = data;
            _appSettings = appSettings;

            BuildUi();

            Load += async (_, _) => await InitializeAsync();
        }

        private Guid CurrentProfileId =>
            (_cboProfile.SelectedItem as ProfileItem)?.Profile.Id ?? Guid.Empty;

        private void BuildUi()
        {
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* icon optional */ }
            Width = 1080;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 9F);

            var top = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 8) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            _lblProfile = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 4, 0) };
            flow.Controls.Add(_lblProfile);
            flow.Controls.Add(_cboProfile);
            _cboProfile.SelectedIndexChanged += async (_, _) => await RefreshActiveTabAsync();

            _btnManage = MakeButton(async (_, _) => await ManageProfilesAsync());
            flow.Controls.Add(_btnManage);
            flow.Controls.Add(new Label { Text = "   ", AutoSize = true });
            _btnOpen = MakeButton((_, _) => OpenDataFolder());
            flow.Controls.Add(_btnOpen);
            _btnExport = MakeButton((_, _) => ExportData());
            flow.Controls.Add(_btnExport);
            _btnImport = MakeButton(async (_, _) => await ImportDataAsync());
            flow.Controls.Add(_btnImport);
            _btnRefresh = MakeButton(async (_, _) => await RefreshActiveTabAsync());
            flow.Controls.Add(_btnRefresh);

            flow.Controls.Add(new Label { Text = "    ", AutoSize = true });
            _lblLang = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 4, 0) };
            flow.Controls.Add(_lblLang);
            _cboLang.Items.Add(new LangItem("en", "English"));
            _cboLang.Items.Add(new LangItem("es", "Español (MX)"));
            _cboLang.SelectedIndex = Loc.Language == "es" ? 1 : 0;
            _cboLang.SelectedIndexChanged += async (_, _) => await OnLanguageChangedAsync();
            flow.Controls.Add(_cboLang);

            top.Controls.Add(flow);

            _tabs.SelectedIndexChanged += async (_, _) => { if (!_suppressTabEvent) await RefreshActiveTabAsync(); };

            Controls.Add(_tabs);
            Controls.Add(top);

            ApplyChromeText();
            BuildTabs();
        }

        private void ApplyChromeText()
        {
            Text = Loc.T("Budget Manager");
            _lblProfile.Text = Loc.T("Profile:");
            _lblLang.Text = Loc.T("Language:");
            _btnManage.Text = Loc.T("Manage Profiles");
            _btnOpen.Text = Loc.T("Open Data Folder");
            _btnExport.Text = Loc.T("Export…");
            _btnImport.Text = Loc.T("Import…");
            _btnRefresh.Text = Loc.T("Refresh");
        }

        private void BuildTabs()
        {
            _suppressTabEvent = true;
            int selected = _tabs.TabPages.Count > 0 ? _tabs.SelectedIndex : 0;

            foreach (TabPage page in _tabs.TabPages) page.Dispose();
            _tabs.TabPages.Clear();

            AddTab(Loc.T("Dashboard"), new DashboardView(_budget, _safeToSpend, _appSettings));
            AddTab(Loc.T("Accounts"), new AccountsView(_accountsCtl));
            AddTab(Loc.T("Transactions"), new TransactionsView(_transactionsCtl, _accountsCtl, _categoriesCtl, _recurringCtl, _runner));
            AddTab(Loc.T("Categories"), new CategoriesView(_categoriesCtl));
            AddTab(Loc.T("Goals"), new GoalsView(_goalsCtl));
            AddTab(Loc.T("Graph"), new GraphView(_projection));
            AddTab(Loc.T("About"), new AboutView());

            if (selected >= 0 && selected < _tabs.TabPages.Count)
                _tabs.SelectedIndex = selected;
            _suppressTabEvent = false;
        }

        private void AddTab(string title, Control view)
        {
            var page = new TabPage(title) { Padding = new Padding(8) };
            view.Dock = DockStyle.Fill;
            page.Controls.Add(view);
            _tabs.TabPages.Add(page);
        }

        private static Button MakeButton(EventHandler onClick)
        {
            var b = new Button { AutoSize = true, Margin = new Padding(4, 2, 0, 2) };
            b.Click += onClick;
            return b;
        }

        private async Task OnLanguageChangedAsync()
        {
            if (_suppressLang) return;
            if (_cboLang.SelectedItem is not LangItem item) return;

            Loc.SetLanguage(item.Code);
            _appSettings.SaveLanguage(item.Code);
            ApplyChromeText();
            BuildTabs();
            await RefreshActiveTabAsync();
        }

        private async Task InitializeAsync()
        {
            await ReloadProfilesAsync();

            // Post any due recurring transactions across all accounts at launch so the
            // dashboard and balances are up to date even before the Transactions tab opens.
            try { await _runner.RunDueAsync(DateTime.Now); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Loc.T("Recurring"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }

            // Post any due savings interest (after recurring, so transfers into savings are reflected).
            try { await _interest.RunDueAsync(DateTime.Now); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Loc.T("Interest"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }

            await RefreshActiveTabAsync();
        }

        private async Task ReloadProfilesAsync()
        {
            var result = await _profiles.GetAll();
            var list = (result.Data ?? Enumerable.Empty<Profile>()).ToList();

            if (list.Count == 0)
            {
                var seed = new Profile { Names = "Default", LastNames = "Profile", Email = "" };
                await _profiles.Add(seed);
                list.Add(seed);
            }

            var previous = CurrentProfileId;

            _cboProfile.Items.Clear();
            foreach (var p in list)
                _cboProfile.Items.Add(new ProfileItem { Profile = p });

            int index = 0;
            for (int i = 0; i < _cboProfile.Items.Count; i++)
            {
                if (((ProfileItem)_cboProfile.Items[i]!).Profile.Id == previous)
                {
                    index = i;
                    break;
                }
            }
            _cboProfile.SelectedIndex = _cboProfile.Items.Count > 0 ? index : -1;
        }

        private async Task RefreshActiveTabAsync()
        {
            if (CurrentProfileId == Guid.Empty) return;

            try
            {
                Control active = _tabs.SelectedTab?.Controls[0]!;
                if (active is IRefreshableView view)
                    await view.LoadAsync(CurrentProfileId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ManageProfilesAsync()
        {
            using var dlg = new Dialogs.ProfilesDialog(_profiles);
            dlg.ShowDialog(this);
            await ReloadProfilesAsync();
            await RefreshActiveTabAsync();
        }

        private void OpenDataFolder()
        {
            try
            {
                Directory.CreateDirectory(_data.DataDirectory);
                Process.Start(new ProcessStartInfo { FileName = _data.DataDirectory, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportData()
        {
            using var dlg = new SaveFileDialog
            {
                Title = Loc.T("Export budget data"),
                Filter = Loc.T("Zip archive (*.zip)|*.zip"),
                FileName = $"budgetmanager-backup-{DateTime.Now:yyyyMMdd-HHmm}.zip"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _data.ExportToZip(dlg.FileName);
                MessageBox.Show(this, Loc.F("Exported {0} data file(s) to:\n{1}", _data.FileCount, dlg.FileName),
                    Loc.T("Export complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Export failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ImportDataAsync()
        {
            using var dlg = new OpenFileDialog
            {
                Title = Loc.T("Import budget data"),
                Filter = Loc.T("Zip archive (*.zip)|*.zip")
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var confirm = MessageBox.Show(this,
                Loc.T("Importing will overwrite existing data files with the contents of the backup. Continue?"),
                Loc.T("Confirm import"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                int restored = _data.ImportFromZip(dlg.FileName);
                await ReloadProfilesAsync();
                await RefreshActiveTabAsync();
                MessageBox.Show(this, Loc.F("Restored {0} data file(s).", restored),
                    Loc.T("Import complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Import failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class ProfileItem
        {
            public Profile Profile { get; init; } = default!;
            public override string ToString() => $"{Profile.Names} {Profile.LastNames}".Trim();
        }

        private sealed record LangItem(string Code, string Display)
        {
            public override string ToString() => Display;
        }
    }
}
