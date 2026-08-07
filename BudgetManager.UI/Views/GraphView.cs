using BudgetManager.BLL.Services;

namespace BudgetManager.UI.Views
{
    /// <summary>
    /// "Graph" tab: a one-month projection of net worth (with recurring items injected),
    /// with controls to slide the start date while always showing a month of data.
    /// </summary>
    public class GraphView : UserControl, IRefreshableView
    {
        private readonly ProjectionService _service;
        private readonly ProjectionChart _chart = new() { Dock = DockStyle.Fill };
        private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Short, Width = 130, Margin = new Padding(6, 6, 6, 0) };

        private Guid _profileId;
        private bool _loading;

        public GraphView(ProjectionService service)
        {
            _service = service;
            BuildUi();
        }

        private void BuildUi()
        {
            var toolbar = Ui.Toolbar(
                Ui.Button(Loc.T("◀ Week"), (_, _) => Shift(-7)),
                Ui.Button(Loc.T("Week ▶"), (_, _) => Shift(7)),
                Ui.Button(Loc.T("◀ Month"), (_, _) => ShiftMonths(-1)),
                Ui.Button(Loc.T("Month ▶"), (_, _) => ShiftMonths(1)),
                Ui.Button(Loc.T("Today"), (_, _) => SetStart(DateTime.Today)));

            toolbar.Controls.Add(new Label { Text = Loc.T("Start:"), AutoSize = true, Margin = new Padding(16, 8, 4, 0) });
            toolbar.Controls.Add(_start);

            var chkCategories = new CheckBox
            {
                Text = Loc.T("Categories"),
                Checked = _chart.ShowCategories,
                AutoSize = true,
                Margin = new Padding(16, 8, 4, 0)
            };
            chkCategories.CheckedChanged += (_, _) => _chart.ShowCategories = chkCategories.Checked;
            toolbar.Controls.Add(chkCategories);

            _start.Value = DateTime.Today.AddMonths(-1);
            _start.ValueChanged += async (_, _) => { if (!_loading) await RebuildAsync(); };

            Controls.Add(_chart);
            Controls.Add(toolbar);
        }

        public async Task LoadAsync(Guid profileId)
        {
            _profileId = profileId;
            await RebuildAsync();
        }

        private void Shift(int days) => SetStart(_start.Value.Date.AddDays(days));
        private void ShiftMonths(int months) => SetStart(_start.Value.Date.AddMonths(months));

        private void SetStart(DateTime date)
        {
            // Clamp to the picker's supported range, then let ValueChanged rebuild.
            if (date < _start.MinDate) date = _start.MinDate;
            if (date > _start.MaxDate) date = _start.MaxDate;
            _start.Value = date;
        }

        private async Task RebuildAsync()
        {
            if (_profileId == Guid.Empty) return;

            _loading = true;
            try
            {
                var series = await _service.BuildAsync(_profileId, _start.Value.Date);
                _chart.SetSeries(series);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Loc.T("Graph"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
