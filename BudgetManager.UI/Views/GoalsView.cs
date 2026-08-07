using BudgetManager.BLL;
using BudgetManager.Domain;
using BudgetManager.UI.Dialogs;

namespace BudgetManager.UI.Views
{
    public class GoalsView : UserControl, IRefreshableView
    {
        private readonly GoalController _controller;
        private readonly DataGridView _grid = Ui.Grid();

        public GoalsView(GoalController controller)
        {
            _controller = controller;
            BuildUi();
        }

        private void BuildUi()
        {
            _grid.Columns.AddRange(
                Ui.Col(nameof(GoalRow.Name), Loc.T("Goal"), fill: 2f),
                Ui.Col(nameof(GoalRow.Target), Loc.T("Target"), "N2"),
                Ui.Col(nameof(GoalRow.Current), Loc.T("Current"), "N2"),
                Ui.Col(nameof(GoalRow.Progress), Loc.T("Progress %"), "N0"),
                Ui.Col(nameof(GoalRow.DueDate), Loc.T("Due")));

            var toolbar = Ui.Toolbar(
                Ui.Button(Loc.T("Add"), async (_, _) => await AddAsync()),
                Ui.Button(Loc.T("Edit"), async (_, _) => await EditAsync()),
                Ui.Button(Loc.T("Delete"), async (_, _) => await DeleteAsync()),
                Ui.Button(Loc.T("Add to goal"), async (_, _) => await AddToGoalAsync()));

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        public async Task LoadAsync(Guid profileId)
        {
            var result = await _controller.GetAll();
            var goals = (result.Data ?? Enumerable.Empty<Goal>()).ToList();

            _grid.DataSource = goals.Select(g => new GoalRow
            {
                Model = g,
                Name = g.Name,
                Target = g.TargetAmount,
                Current = g.CurrentAmount,
                Progress = g.TargetAmount > 0 ? Math.Clamp(g.CurrentAmount / g.TargetAmount * 100m, 0, 100) : 0,
                DueDate = g.DueDate?.ToString("yyyy-MM-dd") ?? ""
            }).ToList();
        }

        private Goal? Selected => (_grid.CurrentRow?.DataBoundItem as GoalRow)?.Model;

        private async Task AddAsync()
        {
            using var dlg = new GoalEditDialog(new Goal());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private async Task EditAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a goal first.")); return; }
            using var dlg = new GoalEditDialog(Clone(Selected));
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private async Task DeleteAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a goal first.")); return; }
            if (MessageBox.Show(this, Loc.F("Delete goal '{0}'?", Selected.Name), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = await _controller.Delete(Selected.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private async Task AddToGoalAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a goal first.")); return; }

            using var dlg = new AmountInputDialog(Loc.T("Add to goal"), Loc.T("Amount"));
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var goal = Clone(Selected);
            goal.CurrentAmount = Math.Max(0m, goal.CurrentAmount + dlg.Amount);

            var r = await _controller.Update(goal);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private static Goal Clone(Goal g) => new()
        {
            Id = g.Id,
            Name = g.Name,
            TargetAmount = g.TargetAmount,
            CurrentAmount = g.CurrentAmount,
            DueDate = g.DueDate
        };

        private void Warn(string message) =>
            MessageBox.Show(this, message, Loc.T("Budget Manager"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private sealed class GoalRow
        {
            public Goal Model { get; set; } = default!;
            public string Name { get; set; } = "";
            public decimal Target { get; set; }
            public decimal Current { get; set; }
            public decimal Progress { get; set; }
            public string DueDate { get; set; } = "";
        }
    }
}
