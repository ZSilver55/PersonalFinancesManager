using System.Drawing;

namespace BudgetManager.UI.Dialogs
{
    /// <summary>Helpers for building simple two-column edit dialogs consistently.</summary>
    internal static class DialogUi
    {
        public static (TableLayoutPanel table, Button ok) Build(Form form, string title, int width = 440, int height = 340)
        {
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ClientSize = new Size(width, height);
            form.Font = new Font("Segoe UI", 9F);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12),
                AutoSize = false
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var ok = new Button { Text = Loc.T("OK"), DialogResult = DialogResult.OK, Width = 90 };
            var cancel = new Button { Text = Loc.T("Cancel"), DialogResult = DialogResult.Cancel, Width = 90 };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 46,
                Padding = new Padding(10)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            form.AcceptButton = ok;
            form.CancelButton = cancel;

            form.Controls.Add(table);
            form.Controls.Add(buttons);
            return (table, ok);
        }

        public static void Row(TableLayoutPanel table, string label, Control input)
        {
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 6, 0) });
            table.Controls.Add(input);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        public static NumericUpDown Money(decimal value, decimal min = -1_000_000_000m)
        {
            var n = new NumericUpDown
            {
                DecimalPlaces = 2,
                Minimum = min,
                Maximum = 1_000_000_000m,
                ThousandsSeparator = true,
                Increment = 1m
            };
            n.Value = Math.Clamp(value, n.Minimum, n.Maximum);
            return n;
        }

        public static ComboBox EnumCombo(Type enumType, object? selected)
        {
            var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var v in Enum.GetValues(enumType))
                cbo.Items.Add(v!);
            if (selected != null) cbo.SelectedItem = selected;
            else if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
            return cbo;
        }

        /// <summary>Combo whose items carry an arbitrary value; a leading "(none)" maps to null.</summary>
        public sealed class Item
        {
            public object? Value { get; init; }
            public string Text { get; init; } = "";
            public override string ToString() => Text;
        }
    }
}
