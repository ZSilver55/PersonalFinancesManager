namespace BudgetManager.UI.Dialogs
{
    /// <summary>Minimal dialog to capture a single line of text.</summary>
    public class TextInputDialog : Form
    {
        private readonly TextBox _text = new();

        public string Value => _text.Text.Trim();

        public TextInputDialog(string title, string label, string initial = "")
        {
            var (table, _) = DialogUi.Build(this, title, width: 480, height: 170);
            _text.Text = initial;
            DialogUi.Row(table, label, _text);
        }
    }
}
