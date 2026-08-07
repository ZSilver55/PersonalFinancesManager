using System.Drawing;
using BudgetManager.BLL;
using BudgetManager.BLL.Services;
using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.UI.Dialogs;

namespace BudgetManager.UI.Views
{
    public class TransactionsView : UserControl, IRefreshableView
    {
        private readonly TransactionsController _transactions;
        private readonly AccountController _accountsController;
        private readonly CategoriesController _categoriesController;
        private readonly RecurringTransactionsController _recurringController;
        private readonly RecurringExecutionService _runner;

        private readonly ComboBox _filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(0, 4, 12, 0) };
        private readonly DataGridView _grid = Ui.Grid();
        private readonly DataGridView _recurGrid = Ui.Grid();
        private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

        private Guid _profileId;
        private List<Account> _accounts = new();
        private List<Category> _categories = new();
        private bool _loading;

        public TransactionsView(
            TransactionsController transactions,
            AccountController accountsController,
            CategoriesController categoriesController,
            RecurringTransactionsController recurringController,
            RecurringExecutionService runner)
        {
            _transactions = transactions;
            _accountsController = accountsController;
            _categoriesController = categoriesController;
            _recurringController = recurringController;
            _runner = runner;
            BuildUi();

            Load += (_, _) =>
            {
                try
                {
                    int lo = _split.Panel1MinSize + 1;
                    int hi = Math.Max(lo, _split.Height - _split.Panel2MinSize - 1);
                    _split.SplitterDistance = Math.Clamp((int)(_split.Height * 0.58), lo, hi);
                }
                catch { /* height not ready */ }
            };
        }

        private Font? _boldFont;

        private void BuildUi()
        {
            _grid.DataBindingComplete += StyleRows;
            _recurGrid.DataBindingComplete += StyleRows;

            // --- transactions (top) ---
            _grid.Columns.AddRange(
                Ui.Col(nameof(TxnRow.Date), Loc.T("Date")),
                Ui.Col(nameof(TxnRow.Type), Loc.T("Type")),
                Ui.Col(nameof(TxnRow.Account), Loc.T("Account"), fill: 2f),
                Ui.Col(nameof(TxnRow.Destination), Loc.T("To (transfer)"), fill: 2f),
                Ui.Col(nameof(TxnRow.Category), Loc.T("Category"), fill: 1.5f),
                Ui.Col(nameof(TxnRow.Amount), Loc.T("Amount"), "N2"),
                Ui.Col(nameof(TxnRow.Description), Loc.T("Description"), fill: 2.5f));

            var txnToolbar = Ui.Toolbar(
                Ui.Button(Loc.T("Add"), async (_, _) => await AddAsync()),
                Ui.Button(Loc.T("Edit"), async (_, _) => await EditAsync()),
                Ui.Button(Loc.T("Delete"), async (_, _) => await DeleteAsync()));
            txnToolbar.Controls.Add(new Label { Text = Loc.T("Filter:"), AutoSize = true, Margin = new Padding(16, 8, 4, 0) });
            txnToolbar.Controls.Add(_filter);
            _filter.SelectedIndexChanged += async (_, _) => { if (!_loading) await ReloadGridAsync(); };

            var txnPanel = new Panel { Dock = DockStyle.Fill };
            txnPanel.Controls.Add(_grid);
            txnPanel.Controls.Add(txnToolbar);
            txnPanel.Controls.Add(Ui.Caption(Loc.T("Transactions")));

            // --- recurring (bottom) ---
            _recurGrid.Columns.AddRange(
                Ui.Col(nameof(RecurRow.Name), Loc.T("Name"), fill: 2f),
                Ui.Col(nameof(RecurRow.Account), Loc.T("Account"), fill: 2f),
                Ui.Col(nameof(RecurRow.Amount), Loc.T("Amount"), "N2"),
                Ui.Col(nameof(RecurRow.Category), Loc.T("Category"), fill: 1.5f),
                Ui.Col(nameof(RecurRow.Frequency), Loc.T("Frequency")),
                Ui.Col(nameof(RecurRow.Next), Loc.T("Next run")),
                Ui.Col(nameof(RecurRow.Enabled), Loc.T("Enabled")));

            var recurToolbar = Ui.Toolbar(
                Ui.Button(Loc.T("Add"), async (_, _) => await AddRecurringAsync()),
                Ui.Button(Loc.T("Edit"), async (_, _) => await EditRecurringAsync()),
                Ui.Button(Loc.T("Delete"), async (_, _) => await DeleteRecurringAsync()),
                Ui.Button(Loc.T("Run due now"), async (_, _) => await RunDueAsync(manual: true)),
                Ui.Button(Loc.T("Enable/Disable"), async (_, _) => await ToggleEnabledAsync()));

            var recurPanel = new Panel { Dock = DockStyle.Fill };
            recurPanel.Controls.Add(_recurGrid);
            recurPanel.Controls.Add(recurToolbar);
            recurPanel.Controls.Add(Ui.Caption(Loc.T("Recurring transactions (auto-posted when due)")));

            _split.Panel1.Controls.Add(txnPanel);
            _split.Panel2.Controls.Add(recurPanel);

            Controls.Add(_split);
        }

        public async Task LoadAsync(Guid profileId)
        {
            _profileId = profileId;

            var accResult = await _accountsController.GetAccounts(profileId);
            _accounts = (accResult.Data ?? Enumerable.Empty<Account>()).ToList();

            var catResult = await _categoriesController.GetAll();
            _categories = (catResult.Data ?? Enumerable.Empty<Category>()).ToList();

            RebuildFilter();

            // Post any due recurring items for this profile before showing the ledger.
            await RunDueAsync(manual: false);

            await ReloadGridAsync();
            await ReloadRecurringAsync();
        }

        private HashSet<Guid> ProfileAccountIds() => _accounts.Select(a => a.Id).ToHashSet();

        // ---------- transactions ----------

        private void RebuildFilter()
        {
            var previous = (_filter.SelectedItem as FilterItem)?.AccountId;

            _loading = true;
            _filter.Items.Clear();
            _filter.Items.Add(new FilterItem { AccountId = null, Display = Loc.T("All accounts") });
            foreach (var a in _accounts)
                _filter.Items.Add(new FilterItem { AccountId = a.Id, Display = a.Name });

            int index = 0;
            for (int i = 0; i < _filter.Items.Count; i++)
                if (((FilterItem)_filter.Items[i]!).AccountId == previous) { index = i; break; }
            _filter.SelectedIndex = index;
            _loading = false;
        }

        private async Task ReloadGridAsync()
        {
            var accountIds = ProfileAccountIds();
            var accountNames = _accounts.ToDictionary(a => a.Id, a => a.Name);
            var categoryNames = _categories.ToDictionary(c => c.Id, c => c.Name);

            var filterAccount = (_filter.SelectedItem as FilterItem)?.AccountId;

            List<Transaction> txns;
            if (filterAccount is Guid accId)
            {
                var r = await _transactions.GetTransactionsByAccount(accId);
                txns = (r.Data ?? Enumerable.Empty<Transaction>()).ToList();
            }
            else
            {
                var r = await _transactions.GetAll();
                txns = (r.Data ?? Enumerable.Empty<Transaction>())
                    .Where(t => accountIds.Contains(t.SourceAccountId)
                                || (t.DestinationAccountId.HasValue && accountIds.Contains(t.DestinationAccountId.Value)))
                    .OrderByDescending(t => t.Date)
                    .ToList();
            }

            var rows = txns.Select(t => new TxnRow
            {
                Model = t,
                Date = t.Date.ToString("yyyy-MM-dd"),
                Type = t.Type.ToString(),
                Account = Name(accountNames, t.SourceAccountId),
                Destination = t.DestinationAccountId.HasValue ? Name(accountNames, t.DestinationAccountId.Value) : "",
                Category = t.CategoryId.HasValue && categoryNames.TryGetValue(t.CategoryId.Value, out var cn) ? cn : "",
                Amount = t.Amount,
                Description = t.Description
            }).ToList();

            if (rows.Count > 0)
            {
                decimal income = txns.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                decimal expense = txns.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

                decimal net;
                string description;
                if (filterAccount is Guid accId2)
                {
                    // Single account: transfers change this account's balance, so include them.
                    decimal transferIn = txns.Where(t => t.Type == TransactionType.Transfer && t.DestinationAccountId == accId2).Sum(t => t.Amount);
                    decimal transferOut = txns.Where(t => t.Type == TransactionType.Transfer && t.SourceAccountId == accId2).Sum(t => t.Amount);
                    net = income - expense + transferIn - transferOut;
                    description = Loc.F("Income {0}  ·  Expense {1}  ·  Transfers +{2}/-{3}  ·  Net {4}",
                        income.ToString("N2"), expense.ToString("N2"), transferIn.ToString("N2"), transferOut.ToString("N2"), net.ToString("N2"));
                }
                else
                {
                    // All accounts: transfers are internal moves that net to zero, so exclude them.
                    net = income - expense;
                    description = Loc.F("Income {0}  ·  Expense {1}  ·  Net {2}",
                        income.ToString("N2"), expense.ToString("N2"), net.ToString("N2"));
                }

                rows.Add(new TxnRow
                {
                    IsSummary = true,
                    Date = Loc.F("{0} txns", txns.Count),
                    Type = Loc.T("TOTAL"),
                    Category = "",
                    Amount = net,
                    Description = description
                });
            }

            _grid.DataSource = rows;
        }

        private static string Name(IReadOnlyDictionary<Guid, string> map, Guid id) =>
            map.TryGetValue(id, out var n) ? n : "—";

        private Transaction? Selected => (_grid.CurrentRow?.DataBoundItem as TxnRow)?.Model;

        private async Task AddAsync()
        {
            if (_accounts.Count == 0) { Warn(Loc.T("Create an account first.")); return; }
            var txn = new Transaction { Date = DateTime.Now, SourceAccountId = _accounts[0].Id };
            using var dlg = new TransactionEditDialog(txn, _accounts, _categories);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _transactions.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await ReloadGridAsync();
        }

        private async Task EditAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a transaction first.")); return; }
            using var dlg = new TransactionEditDialog(CloneTxn(Selected), _accounts, _categories);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _transactions.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await ReloadGridAsync();
        }

        private async Task DeleteAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a transaction first.")); return; }
            if (MessageBox.Show(this, Loc.T("Delete this transaction?"), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = await _transactions.Delete(Selected.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await ReloadGridAsync();
        }

        // ---------- recurring ----------

        private async Task ReloadRecurringAsync()
        {
            var accountIds = ProfileAccountIds();
            var accountNames = _accounts.ToDictionary(a => a.Id, a => a.Name);
            var categoryNames = _categories.ToDictionary(c => c.Id, c => c.Name);

            var r = await _recurringController.GetAll();
            var items = (r.Data ?? Enumerable.Empty<RecurringTransaction>())
                .Where(x => accountIds.Contains(x.AccountId))
                .ToList();

            var rows = new List<RecurRow>();

            // Grouped by Enabled: a header (with subtotal) per group, then that group's items.
            foreach (var enabled in new[] { true, false })
            {
                var group = items
                    .Where(x => x.Enabled == enabled)
                    .OrderBy(x => x.NextExecution ?? DateTime.MaxValue)
                    .ToList();

                if (group.Count == 0) continue;

                rows.Add(new RecurRow
                {
                    IsGroupHeader = true,
                    Name = Loc.T(enabled ? "Enabled" : "Disabled"),
                    Frequency = Loc.F("{0} item(s)", group.Count),
                    Amount = group.Sum(x => x.Amount)
                });

                rows.AddRange(group.Select(x => new RecurRow
                {
                    Model = x,
                    Name = x.Name,
                    Account = Name(accountNames, x.AccountId),
                    Amount = x.Amount,
                    Category = x.DestinationAccountId.HasValue
                        ? "→ " + Name(accountNames, x.DestinationAccountId.Value)
                        : (x.CategoryId.HasValue && categoryNames.TryGetValue(x.CategoryId.Value, out var cn) ? cn : ""),
                    Frequency = x.Frequency.ToString(),
                    Next = x.NextExecution?.ToString("yyyy-MM-dd") ?? "",
                    Enabled = Loc.T(x.Enabled ? "Yes" : "No")
                }));
            }

            if (items.Count > 0)
            {
                rows.Add(new RecurRow
                {
                    IsSummary = true,
                    Name = Loc.T("TOTAL"),
                    Frequency = Loc.F("{0} item(s)", items.Count),
                    Amount = items.Sum(x => x.Amount)
                });
            }

            _recurGrid.DataSource = rows;
        }

        private RecurringTransaction? SelectedRecurring => (_recurGrid.CurrentRow?.DataBoundItem as RecurRow)?.Model;

        private async Task AddRecurringAsync()
        {
            if (_accounts.Count == 0) { Warn(Loc.T("Create an account first.")); return; }
            var model = new RecurringTransaction
            {
                AccountId = _accounts[0].Id,
                NextExecution = DateTime.Today,
                Enabled = true
            };
            using var dlg = new RecurringEditDialog(model, _accounts, _categories);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _recurringController.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await RunDueAsync(manual: false);
            await ReloadGridAsync();
            await ReloadRecurringAsync();
        }

        private async Task EditRecurringAsync()
        {
            if (SelectedRecurring is null) { Warn(Loc.T("Select a recurring item first.")); return; }
            using var dlg = new RecurringEditDialog(CloneRecurring(SelectedRecurring), _accounts, _categories);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _recurringController.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await ReloadRecurringAsync();
        }

        private async Task DeleteRecurringAsync()
        {
            if (SelectedRecurring is null) { Warn(Loc.T("Select a recurring item first.")); return; }
            if (MessageBox.Show(this, Loc.F("Delete recurring item '{0}'?", SelectedRecurring.Name), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = await _recurringController.Delete(SelectedRecurring.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await ReloadRecurringAsync();
        }

        private async Task ToggleEnabledAsync()
        {
            if (SelectedRecurring is null) { Warn(Loc.T("Select a recurring item first.")); return; }

            var updated = CloneRecurring(SelectedRecurring);
            updated.Enabled = !updated.Enabled;

            var r = await _recurringController.Update(updated);
            if (!r.Success) { Warn(r.Message); return; }

            // Re-enabling can make a past-due item eligible again.
            if (updated.Enabled) await RunDueAsync(manual: false);

            await ReloadGridAsync();
            await ReloadRecurringAsync();
        }

        private async Task RunDueAsync(bool manual)
        {
            int created = await _runner.RunDueAsync(DateTime.Now, ProfileAccountIds());
            if (manual)
            {
                await ReloadGridAsync();
                await ReloadRecurringAsync();
                MessageBox.Show(this, created == 0 ? Loc.T("Nothing was due.") : Loc.F("Posted {0} transaction(s).", created),
                    Loc.T("Recurring"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ---------- helpers ----------

        private static Transaction CloneTxn(Transaction t) => new()
        {
            Id = t.Id,
            SourceAccountId = t.SourceAccountId,
            DestinationAccountId = t.DestinationAccountId,
            CategoryId = t.CategoryId,
            Tags = t.Tags,
            Amount = t.Amount,
            Type = t.Type,
            Date = t.Date,
            Description = t.Description
        };

        private static RecurringTransaction CloneRecurring(RecurringTransaction x) => new()
        {
            Id = x.Id,
            AccountId = x.AccountId,
            DestinationAccountId = x.DestinationAccountId,
            Name = x.Name,
            Amount = x.Amount,
            CategoryId = x.CategoryId,
            Frequency = x.Frequency,
            NextExecution = x.NextExecution,
            EndDate = x.EndDate,
            Enabled = x.Enabled
        };

        private void StyleRows(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            var grid = (DataGridView)sender!;
            _boldFont ??= new Font(grid.Font, FontStyle.Bold);

            foreach (DataGridViewRow row in grid.Rows)
            {
                var (summary, header) = row.DataBoundItem switch
                {
                    TxnRow t => (t.IsSummary, false),
                    RecurRow r => (r.IsSummary, r.IsGroupHeader),
                    _ => (false, false)
                };

                if (summary)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(226, 226, 226);
                    row.DefaultCellStyle.Font = _boldFont;
                }
                else if (header)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(222, 235, 247);
                    row.DefaultCellStyle.Font = _boldFont;
                }
            }
        }

        private void Warn(string message) =>
            MessageBox.Show(this, message, Loc.T("Budget Manager"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private sealed class FilterItem
        {
            public Guid? AccountId { get; set; }
            public string Display { get; set; } = "";
            public override string ToString() => Display;
        }

        private sealed class TxnRow
        {
            public Transaction Model { get; set; } = default!;
            public string Date { get; set; } = "";
            public string Type { get; set; } = "";
            public string Account { get; set; } = "";
            public string Destination { get; set; } = "";
            public string Category { get; set; } = "";
            public decimal Amount { get; set; }
            public string Description { get; set; } = "";
            public bool IsSummary { get; set; }
        }

        private sealed class RecurRow
        {
            public RecurringTransaction Model { get; set; } = default!;
            public string Name { get; set; } = "";
            public string Account { get; set; } = "";
            public decimal Amount { get; set; }
            public string Category { get; set; } = "";
            public string Frequency { get; set; } = "";
            public string Next { get; set; } = "";
            public string Enabled { get; set; } = "";
            public bool IsSummary { get; set; }
            public bool IsGroupHeader { get; set; }
        }
    }
}
