namespace BudgetManager.UI.Views
{
    /// <summary>A tab view that (re)loads its data for the currently selected profile.</summary>
    public interface IRefreshableView
    {
        Task LoadAsync(Guid profileId);
    }
}
