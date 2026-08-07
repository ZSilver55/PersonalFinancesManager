using System.Drawing;
using BudgetManager.BLL.Services;
using BudgetManager.Domain;
using BudgetManager.UI.Dialogs;

namespace BudgetManager.UI.Views
{
    /// <summary>Read-only overview: safe-to-spend, net worth, month income/expense, balances and goals.</summary>
    public class DashboardView : UserControl, IRefreshableView
    {
        private readonly BudgetService _service;
        private readonly SafeToSpendService _safe;
        private readonly AppSettingsService _appSettings;
        private Guid _profileId;

        private readonly Label _safeTitle = new() { Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        private readonly Label _safeAmount = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 22F, FontStyle.Bold) };
        private readonly Label _safeBreakdown = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(90, 90, 90) };
        private readonly Button _btnConfigSafe = new() { AutoSize = true, Dock = DockStyle.Top };

        private readonly Label _netWorth = Card("Net worth");
        private readonly Label _income = Card("Income (this month)");
        private readonly Label _expense = Card("Expense (this month)");
        private readonly Label _net = Card("Net (this month)");

        private readonly DataGridView _balances = Ui.Grid();
        private readonly FlowLayoutPanel _goals = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };

        public DashboardView(BudgetService service, SafeToSpendService safe, AppSettingsService appSettings)
        {
            _service = service;
            _safe = safe;
            _appSettings = appSettings;
            BuildUi();

            // SplitterDistance must be set once the control has a real width.
            Load += (_, _) =>
            {
                try
                {
                    int lo = _split.Panel1MinSize + 1;
                    int hi = Math.Max(lo, _split.Width - _split.Panel2MinSize - 1);
                    _split.SplitterDistance = Math.Clamp((int)(_split.Width * 0.62), lo, hi);
                }
                catch { /* width not ready yet; keep default */ }
            };
        }

        private void BuildUi()
        {
            var summary = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 96, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 8) };
            summary.Controls.AddRange(new Control[] { CardHost(_netWorth), CardHost(_income), CardHost(_expense), CardHost(_net) });

            _balances.Columns.AddRange(
                Ui.Col(nameof(BalanceRow.Name), Loc.T("Account"), fill: 2f),
                Ui.Col(nameof(BalanceRow.Type), Loc.T("Type")),
                Ui.Col(nameof(BalanceRow.Currency), Loc.T("Currency")),
                Ui.Col(nameof(BalanceRow.Balance), Loc.T("Balance"), "N2"));

            var left = new Panel { Dock = DockStyle.Fill };
            left.Controls.Add(_balances);
            left.Controls.Add(Ui.Caption(Loc.T("Account balances")));

            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(_goals);
            right.Controls.Add(Ui.Caption(Loc.T("Goals")));

            _split.Panel1.Controls.Add(left);
            _split.Panel2.Controls.Add(right);

            Controls.Add(_split);
            Controls.Add(summary);
            Controls.Add(BuildSafeCard()); // added last => docks at the very top
        }

        private Control BuildSafeCard()
        {
            var host = new Panel { Dock = DockStyle.Top, Height = 118, Padding = new Padding(10, 6, 10, 8), BorderStyle = BorderStyle.FixedSingle };

            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 300 };
            leftPanel.Controls.Add(_safeAmount);
            leftPanel.Controls.Add(_safeTitle);

            _btnConfigSafe.Text = Loc.T("Configure…");
            _btnConfigSafe.Click += async (_, _) => await ConfigureSafeAsync();
            var rightPanel = new Panel { Dock = DockStyle.Right, Width = 110 };
            rightPanel.Controls.Add(_btnConfigSafe);

            _safeBreakdown.Padding = new Padding(12, 2, 0, 0);

            host.Controls.Add(_safeBreakdown); // fill (added first)
            host.Controls.Add(leftPanel);
            host.Controls.Add(rightPanel);
            return host;
        }

        private async Task ConfigureSafeAsync()
        {
            var settings = _appSettings.LoadSettings();
            using var dlg = new SafeToSpendConfigDialog(settings.SafetyBuffer, settings.ReserveForGoals);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            settings.SafetyBuffer = dlg.Buffer;
            settings.ReserveForGoals = dlg.ReserveGoals;
            _appSettings.Save(settings);

            if (_profileId != Guid.Empty) await LoadAsync(_profileId);
        }

        private void UpdateSafeCard(SafeToSpendResult s)
        {
            _safeTitle.Text = Loc.T("Safe to spend today");
            _safeAmount.Text = s.RemainingToday.ToString("N2");
            _safeAmount.ForeColor = s.RemainingToday < 0m ? Color.Firebrick : Color.FromArgb(39, 174, 96);

            string horizon = s.NextIncomeDate is DateTime d
                ? Loc.F("{0} days to {1}", s.DaysToRefill, d.ToString("yyyy-MM-dd"))
                : Loc.F("{0} days to {1}", s.DaysToRefill, s.HorizonEnd.ToString("yyyy-MM-dd"));

            var lines = new List<string>
            {
                horizon,
                $"{Loc.T("Net worth")} {s.NetWorth:N2}   ·   {Loc.T("Bills")} -{s.FutureBills:N2}   ·   {Loc.T("Safety buffer")} -{s.SafetyBuffer:N2}   ·   {Loc.T("Goal reserve/day")} -{s.GoalDailyReserve:N2}",
                Loc.F("Daily allowance {0}  ·  Spent today {1}", s.SafePerDay.ToString("N2"), s.SpentToday.ToString("N2"))
            };
            if (s.Overcommitted) lines.Add(Loc.F("Over-committed by {0}", s.OvercommittedBy.ToString("N2")));
            if (s.NextIncomeDate is null) lines.Add(Loc.T("No upcoming income detected — using end of month."));
            if (s.MixedCurrencies) lines.Add(Loc.T("Mixed currencies — showing combined totals."));

            _safeBreakdown.Text = string.Join(Environment.NewLine, lines);
        }

        public async Task LoadAsync(Guid profileId)
        {
            _profileId = profileId;

            var prefs = _appSettings.LoadSettings();
            var safe = await _safe.ComputeAsync(profileId, prefs.SafetyBuffer, prefs.ReserveForGoals);
            UpdateSafeCard(safe);

            var now = DateTime.Now;
            var from = new DateTime(now.Year, now.Month, 1);
            var to = from.AddMonths(1).AddTicks(-1);

            var d = await _service.GetDashboardAsync(profileId, from, to);

            SetCard(_netWorth, Loc.T("Net worth"), d.NetWorth);
            SetCard(_income, Loc.T("Income (this month)"), d.PeriodIncome);
            SetCard(_expense, Loc.T("Expense (this month)"), d.PeriodExpense);
            SetCard(_net, Loc.T("Net (this month)"), d.PeriodNet);

            _balances.DataSource = d.Balances
                .Select(b => new BalanceRow
                {
                    Name = b.Account.Name,
                    Type = b.Account.Type.ToString(),
                    Currency = b.Account.Currency,
                    Balance = b.Balance
                })
                .ToList();

            _goals.Controls.Clear();
            if (d.Goals.Count == 0)
            {
                _goals.Controls.Add(new Label { Text = Loc.T("No goals yet."), AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(4) });
            }
            else
            {
                foreach (var g in d.Goals)
                    _goals.Controls.Add(GoalCard(g));
            }
        }

        private static Control GoalCard(Domain.Goal g)
        {
            int pct = g.TargetAmount > 0
                ? (int)Math.Clamp(Math.Round(g.CurrentAmount / g.TargetAmount * 100m), 0, 100)
                : 0;

            var panel = new Panel { Width = 380, Height = 60, Margin = new Padding(4), BorderStyle = BorderStyle.FixedSingle };
            var title = new Label { Text = $"{g.Name}", AutoSize = true, Location = new Point(6, 6), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var detail = new Label { Text = $"{g.CurrentAmount:N2} / {g.TargetAmount:N2}  ({pct}%)", AutoSize = true, Location = new Point(6, 24) };
            var bar = new ProgressBar { Location = new Point(6, 42), Width = 360, Height = 12, Minimum = 0, Maximum = 100, Value = pct };
            panel.Controls.Add(title);
            panel.Controls.Add(detail);
            panel.Controls.Add(bar);
            return panel;
        }

        private static Label Card(string _) => new()
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };

        private static Panel CardHost(Label value)
        {
            var host = new Panel { Width = 210, Height = 80, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(10, 6, 10, 6), BorderStyle = BorderStyle.FixedSingle };
            host.Controls.Add(value);
            return host;
        }

        private static void SetCard(Label value, string caption, decimal amount)
        {
            value.Text = $"{caption}\r\n{amount:N2}";
            value.ForeColor = amount < 0 ? Color.Firebrick : SystemColors.ControlText;
        }

        private sealed class BalanceRow
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string Currency { get; set; } = "";
            public decimal Balance { get; set; }
        }
    }
}
