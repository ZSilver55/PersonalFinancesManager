using BudgetManager.BLL;
using BudgetManager.Domain;
using BudgetManager.UI.Dialogs;

namespace BudgetManager.UI.Views
{
    public class CategoriesView : UserControl, IRefreshableView
    {
        private readonly CategoriesController _controller;
        private readonly DataGridView _grid = Ui.Grid();
        private List<Category> _all = new();

        public CategoriesView(CategoriesController controller)
        {
            _controller = controller;
            BuildUi();
        }

        private void BuildUi()
        {
            _grid.Columns.AddRange(
                Ui.Col(nameof(CategoryRow.Name), Loc.T("Name"), fill: 2f),
                Ui.Col(nameof(CategoryRow.Type), Loc.T("Type")),
                Ui.Col(nameof(CategoryRow.Parent), Loc.T("Parent"), fill: 2f),
                Ui.Col(nameof(CategoryRow.Color), Loc.T("Color")),
                Ui.Col(nameof(CategoryRow.Icon), Loc.T("Icon")));

            var toolbar = Ui.Toolbar(
                Ui.Button(Loc.T("Add"), async (_, _) => await AddAsync()),
                Ui.Button(Loc.T("Edit"), async (_, _) => await EditAsync()),
                Ui.Button(Loc.T("Delete"), async (_, _) => await DeleteAsync()));

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        public async Task LoadAsync(Guid profileId)
        {
            var result = await _controller.GetAll();
            _all = (result.Data ?? Enumerable.Empty<Category>()).ToList();

            var byId = _all.ToDictionary(c => c.Id, c => c.Name);
            _grid.DataSource = _all.Select(c => new CategoryRow
            {
                Model = c,
                Name = c.Name,
                Type = c.Type?.ToString() ?? "",
                Parent = c.ParentCategoryId.HasValue && byId.TryGetValue(c.ParentCategoryId.Value, out var n) ? n : "",
                Color = c.Color,
                Icon = c.Icon
            }).ToList();
        }

        private Category? Selected => (_grid.CurrentRow?.DataBoundItem as CategoryRow)?.Model;

        private async Task AddAsync()
        {
            using var dlg = new CategoryEditDialog(new Category(), _all);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private async Task EditAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a category first.")); return; }
            using var dlg = new CategoryEditDialog(Clone(Selected), _all);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private async Task DeleteAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a category first.")); return; }
            if (MessageBox.Show(this, Loc.F("Delete category '{0}'?", Selected.Name), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = await _controller.Delete(Selected.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync(Guid.Empty);
        }

        private static Category Clone(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type,
            ParentCategoryId = c.ParentCategoryId,
            Color = c.Color,
            Icon = c.Icon
        };

        private void Warn(string message) =>
            MessageBox.Show(this, message, Loc.T("Budget Manager"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private sealed class CategoryRow
        {
            public Category Model { get; set; } = default!;
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string Parent { get; set; } = "";
            public string Color { get; set; } = "";
            public string Icon { get; set; } = "";
        }
    }
}
