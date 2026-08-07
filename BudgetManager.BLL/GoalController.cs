using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    /// <summary>
    /// CRUD for savings goals. Inherits Add/Update/Delete/GetAll/GetById from BaseController.
    /// </summary>
    public class GoalController : BaseController<Goal>
    {
        public GoalController(CommnadHandler commnadHandler, IJsonStore<Goal> store)
            : base(commnadHandler, store)
        {
        }
    }
}
