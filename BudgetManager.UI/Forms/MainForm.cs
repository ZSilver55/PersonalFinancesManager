using System.Diagnostics;
using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.UI.Services;
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
        private readonly DataSourceSwitchService _switch;
        private readonly DesktopAuthService _auth;

        private readonly ComboBox _cboProfile = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

        private Label _lblProfile = null!;
        private Button _btnManage = null!, _btnOpen = null!, _btnExport = null!, _btnImport = null!, _btnRefresh = null!, _btnSettings = null!;
        private Button? _btnSignIn;
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
            AppSettingsService appSettings,
            DataSourceSwitchService switchService,
            DesktopAuthService auth)
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
            _switch = switchService;
            _auth = auth;

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
            _btnSettings = MakeButton(async (_, _) => await OpenSettingsAsync());
            flow.Controls.Add(_btnSettings);

            // Sign-in only makes sense online (API mode); it authenticates against the server's provider.
            var st = _appSettings.LoadSettings();
            bool online = st.PersistenceMode == PersistenceMode.Api && !string.IsNullOrWhiteSpace(st.ApiBaseUrl);
            if (online)
            {
                _btnSignIn = MakeButton(async (_, _) => await ToggleSignInAsync());
                flow.Controls.Add(_btnSignIn);
                _auth.StateChanged += () => { if (IsHandleCreated) BeginInvoke((Action)UpdateSignInText); };
            }

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
            _btnManage.Text = Loc.T("Manage Profiles");
            _btnOpen.Text = Loc.T("Open Data Folder");
            _btnExport.Text = Loc.T("Export…");
            _btnImport.Text = Loc.T("Import…");
            _btnRefresh.Text = Loc.T("Refresh");
            _btnSettings.Text = Loc.T("Settings…");
            UpdateSignInText();
        }

        private void UpdateSignInText()
        {
            if (_btnSignIn is null) return;
            _btnSignIn.Text = _auth.IsSignedIn ? Loc.T("Sign out") : Loc.T("Sign in");
        }

        private async Task ToggleSignInAsync()
        {
            try
            {
                if (_auth.IsSignedIn)
                {
                    _auth.SignOut();
                }
                else
                {
                    Cursor = Cursors.WaitCursor;
                    await _auth.SignInAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Sign in"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                UpdateSignInText();
            }

            await RefreshActiveTabAsync();
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

        private async Task OpenSettingsAsync()
        {
            var s = _appSettings.LoadSettings();
            bool currentOnline = s.PersistenceMode == PersistenceMode.Api && !string.IsNullOrWhiteSpace(s.ApiBaseUrl);
            var currentMode = currentOnline ? PersistenceMode.Api : PersistenceMode.Json;
            string? currentUrl = s.ApiBaseUrl;
            string currentLang = s.Language ?? "en";

            using var dlg = new Dialogs.SettingsDialog(s, currentOnline);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            s.Language = dlg.Language;
            s.SafetyBuffer = dlg.SafetyBuffer;
            s.ReserveForGoals = dlg.ReserveForGoals;
            s.ApiBaseUrl = string.IsNullOrWhiteSpace(dlg.ApiBaseUrl) ? null : dlg.ApiBaseUrl.Trim();

            var targetMode = dlg.Online ? PersistenceMode.Api : PersistenceMode.Json;
            bool urlChanged = !string.Equals(currentUrl, s.ApiBaseUrl, StringComparison.OrdinalIgnoreCase);

            // Switching between offline (JSON) and online (API): migrate the data, then restart
            // so the new store is wired at startup.
            if (targetMode != currentMode)
            {
                if (MessageBox.Show(this,
                        Loc.T("Switching online/offline migrates your data and restarts the app. Continue?"),
                        Loc.T("Settings"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                    return;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    int migrated = await _switch.MigrateAsync(s, currentMode, targetMode);
                    s.PersistenceMode = targetMode;
                    _appSettings.Save(s);
                    MessageBox.Show(this, Loc.F("Migrated {0} record(s). The app will restart now.", migrated),
                        Loc.T("Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Loc.T("Settings"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return; // don't switch/restart on failure
                }
                finally { Cursor = Cursors.Default; }

                Application.Restart();
                return;
            }

            // Still online but the API address changed → restart to reconnect (no migration).
            if (targetMode == PersistenceMode.Api && urlChanged)
            {
                s.PersistenceMode = targetMode;
                _appSettings.Save(s);
                MessageBox.Show(this, Loc.T("The app will restart to apply the new API address."),
                    Loc.T("Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
                return;
            }

            // No data-source change → save and apply live (language, safe-to-spend).
            s.PersistenceMode = currentMode;
            _appSettings.Save(s);

            if (!string.Equals(currentLang, s.Language, StringComparison.OrdinalIgnoreCase))
            {
                Loc.SetLanguage(s.Language);
                ApplyChromeText();
                BuildTabs();
            }
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
    }
}
