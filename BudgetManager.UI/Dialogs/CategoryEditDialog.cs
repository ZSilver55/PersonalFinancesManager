using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;

namespace BudgetManager.UI.Dialogs
{
    public class CategoryEditDialog : Form
    {
        private readonly Category _category;
        private readonly TextBox _name = new();
        private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _parent = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _color = new();
        private readonly TextBox _icon = new();

        public Category Result => _category;

        public CategoryEditDialog(Category category, IEnumerable<Category> all)
        {
            _category = category;
            var (table, _) = DialogUi.Build(this, Loc.T(category.Id == Guid.Empty ? "New category" : "Edit category"), height: 320);

            _name.Text = category.Name ?? "";
            _color.Text = category.Color ?? "";
            _icon.Text = category.Icon ?? "";

            // Type (nullable)
            _type.Items.Add(new DialogUi.Item { Value = null, Text = Loc.T("(none)") });
            foreach (CategoryType v in Enum.GetValues(typeof(CategoryType)))
                _type.Items.Add(new DialogUi.Item { Value = v, Text = v.ToString() });
            _type.SelectedIndex = category.Type.HasValue
                ? IndexOfValue(_type, category.Type.Value) : 0;

            // Parent (exclude self to avoid a cycle)
            _parent.Items.Add(new DialogUi.Item { Value = null, Text = Loc.T("(none)") });
            foreach (var c in all.Where(c => c.Id != category.Id))
                _parent.Items.Add(new DialogUi.Item { Value = c.Id, Text = c.Name });
            _parent.SelectedIndex = category.ParentCategoryId.HasValue
                ? Math.Max(0, IndexOfValue(_parent, category.ParentCategoryId.Value)) : 0;

            DialogUi.Row(table, Loc.T("Name"), _name);
            DialogUi.Row(table, Loc.T("Type"), _type);
            DialogUi.Row(table, Loc.T("Parent"), _parent);
            DialogUi.Row(table, Loc.T("Color"), _color);
            DialogUi.Row(table, Loc.T("Icon"), _icon);

            FormClosing += OnClosing;
        }

        private static int IndexOfValue(ComboBox cbo, object value)
        {
            for (int i = 0; i < cbo.Items.Count; i++)
                if (cbo.Items[i] is DialogUi.Item it && Equals(it.Value, value)) return i;
            return 0;
        }

        private void OnClosing(object? sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                e.Cancel = true;
                MessageBox.Show(this, Loc.T("Name is required."), Loc.T("Validation"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _category.Name = _name.Text.Trim();
            _category.Type = (_type.SelectedItem as DialogUi.Item)?.Value as CategoryType?;
            _category.ParentCategoryId = (_parent.SelectedItem as DialogUi.Item)?.Value as Guid?;
            _category.Color = _color.Text.Trim();
            _category.Icon = _icon.Text.Trim();
        }
    }
}
