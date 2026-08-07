namespace BudgetManager.Queries.Common
{
    /// <summary>
    /// The user whose data the current operation is scoped to. In the API this is derived from
    /// the authenticated token; in single-user hosts it is the empty "local" user.
    /// </summary>
    public interface ICurrentUser
    {
        Guid UserId { get; }
    }

    /// <summary>Default single-user identity (Guid.Empty) for hosts without authentication.</summary>
    public sealed class SystemCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
    }
}
