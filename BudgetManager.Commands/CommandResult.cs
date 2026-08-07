namespace BudgetManager.Commands
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public static CommandResult OK() => new CommandResult { Success = true };
        public static CommandResult Failed(string message) => new CommandResult() { Success = false, Message = message };
        public static CommandResult Failed(Exception ex) => new CommandResult() { Success = false, Message = ex.Message };
        public static CommandResult Failed(ArgumentException ex) => Failed(ex.Message);
        public static CommandResult Failed(NullReferenceException ex) => Failed(ex.Message);
        public static CommandResult Failed(ArgumentNullException ex) => Failed(ex.Message);
    }
}
