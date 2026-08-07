using Microsoft.Extensions.Logging;

namespace BudgetManager.Commands
{
    public class CommnadHandler
    {
        ILogger<CommnadHandler> _logger;
        public CommnadHandler(ILogger<CommnadHandler> logger)
        {
            _logger = logger;
        }
        public async Task<CommandResult> HandleAsync<TParameters>(ICommand<TParameters> command, TParameters parameters)
        {
            try
            {
                return await command.ExecuteAsync(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CommandResult.Failed(ex);
            }
        }
    }
}
