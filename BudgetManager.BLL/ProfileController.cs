using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.BLL
{
    /// <summary>
    /// CRUD for profiles. Inherits Add/Update/Delete/GetAll/GetById from BaseController.
    /// </summary>
    public class ProfileController : BaseController<Profile>
    {
        public ProfileController(CommnadHandler commnadHandler, IJsonStore<Profile> store)
            : base(commnadHandler, store)
        {
        }
    }
}
