using System.Drawing;
using BudgetManager.BLL;
using BudgetManager.Domain;

namespace BudgetManager.UI.Dialogs
{
    /// <summary>Simple management window for profiles (add / edit / delete).</summary>
    public class ProfilesDialog : Form
    {
        private readonly ProfileController _controller;
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        public ProfilesDialog(ProfileController controller)
        {
            _controller = controller;

            Text = Loc.T("Manage profiles");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 360);
            Font = new Font("Segoe UI", 9F);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Profile.Names), HeaderText = Loc.T("First names"), SortMode = DataGridViewColumnSortMode.NotSortable });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Profile.LastNames), HeaderText = Loc.T("Last names"), SortMode = DataGridViewColumnSortMode.NotSortable });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Profile.Email), HeaderText = Loc.T("Email"), SortMode = DataGridViewColumnSortMode.NotSortable });

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 4, 0, 4) };
            bar.Controls.Add(MakeButton(Loc.T("Add"), async (_, _) => await AddAsync()));
            bar.Controls.Add(MakeButton(Loc.T("Edit"), async (_, _) => await EditAsync()));
            bar.Controls.Add(MakeButton(Loc.T("Delete"), async (_, _) => await DeleteAsync()));
            bar.Controls.Add(MakeButton(Loc.T("Close"), (_, _) => Close()));

            Controls.Add(_grid);
            Controls.Add(bar);

            Shown += async (_, _) => await LoadAsync();
        }

        private Profile? Selected => _grid.CurrentRow?.DataBoundItem as Profile;

        private async Task LoadAsync()
        {
            var result = await _controller.GetAll();
            _grid.DataSource = (result.Data ?? Enumerable.Empty<Profile>()).ToList();
        }

        private async Task AddAsync()
        {
            using var dlg = new ProfileEditDialog(new Profile());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Add(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync();
        }

        private async Task EditAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a profile first.")); return; }
            using var dlg = new ProfileEditDialog(Clone(Selected));
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var r = await _controller.Update(dlg.Result);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (Selected is null) { Warn(Loc.T("Select a profile first.")); return; }
            if (MessageBox.Show(this, Loc.F("Delete profile '{0} {1}'?", Selected.Names, Selected.LastNames), Loc.T("Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var r = await _controller.Delete(Selected.Id);
            if (!r.Success) { Warn(r.Message); return; }
            await LoadAsync();
        }

        private static Profile Clone(Profile p) => new()
        {
            Id = p.Id,
            Names = p.Names,
            LastNames = p.LastNames,
            Email = p.Email
        };

        private static Button MakeButton(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, AutoSize = true, Margin = new Padding(0, 0, 6, 0), Padding = new Padding(6, 2, 6, 2) };
            b.Click += onClick;
            return b;
        }

        private void Warn(string message) =>
            MessageBox.Show(this, message, Loc.T("Profiles"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
