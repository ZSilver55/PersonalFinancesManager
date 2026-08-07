using System.Drawing;

namespace BudgetManager.UI.Views
{
    /// <summary>Small factory helpers to keep the WinForms wiring concise and consistent.</summary>
    internal static class Ui
    {
        public static DataGridView Grid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
        }

        public static DataGridViewTextBoxColumn Col(string prop, string header, string? format = null, float fill = 1f)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText = header,
                FillWeight = fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            if (format != null)
            {
                col.DefaultCellStyle.Format = format;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            return col;
        }

        public static Panel Toolbar(params Button[] buttons)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 4, 0, 4),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            panel.Controls.AddRange(buttons);
            return panel;
        }

        public static Button Button(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 6, 0), Padding = new Padding(6, 2, 6, 2) };
            b.Click += onClick;
            return b;
        }

        public static Label Caption(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(2, 2, 0, 0)
            };
        }
    }
}
