using System.Drawing;

namespace BudgetManager.UI.Views
{
    /// <summary>Static "About / Copyright" tab: app info, copyright, license, contact, credits.</summary>
    public class AboutView : UserControl, IRefreshableView
    {
        private const string AppName = "Budget Manager";
        private const string Version = "1.0";
        private const string CopyrightHolder = "Rubén Zaleta Cabrera";
        private const string ContactEmail = "zaleta55+budgetManager@gmail.com";

        public AboutView()
        {
            BuildUi();
        }

        // Static content, but implements the tab contract so the shell can activate it.
        public Task LoadAsync(Guid profileId) => Task.CompletedTask;

        private void BuildUi()
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20, 16, 20, 16)
            };

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon is not null)
                    flow.Controls.Add(new PictureBox
                    {
                        Image = icon.ToBitmap(),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Size = new Size(48, 48),
                        Margin = new Padding(0, 0, 0, 8)
                    });
            }
            catch { /* icon optional */ }

            flow.Controls.Add(Title(AppName));
            flow.Controls.Add(Text(Loc.F("Version {0}", Version)));
            flow.Controls.Add(Spacer());

            flow.Controls.Add(Text($"© {DateTime.Now.Year} {CopyrightHolder}"));
            flow.Controls.Add(Text(Loc.T("All rights reserved.")));
            flow.Controls.Add(Spacer());

            flow.Controls.Add(Heading(Loc.T("Contact")));
            flow.Controls.Add(Text(ContactEmail));
            flow.Controls.Add(Spacer());

            flow.Controls.Add(Heading(Loc.T("Acknowledgements")));
            flow.Controls.Add(Body(Loc.T(
                "Built with .NET and Windows Forms. Uses System.Text.Json for local storage, " +
                "Microsoft.Extensions (Dependency Injection, Logging, Options), and Dapper with " +
                "Microsoft.Data.SqlClient for the optional SQL persistence. Charts and the app icon " +
                "are rendered with GDI+. This software is provided \"as is\", without warranty of any kind.")));

            Controls.Add(flow);
        }

        private static Label Title(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2)
        };

        private static Label Heading(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 2)
        };

        private static Label Text(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 1, 0, 1)
        };

        private static Label Body(string text) => new()
        {
            Text = text,
            AutoSize = false,
            MaximumSize = new Size(640, 0),
            Size = new Size(640, 90),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 1, 0, 1)
        };

        private static Label Spacer() => new() { AutoSize = false, Size = new Size(1, 8) };
    }
}
