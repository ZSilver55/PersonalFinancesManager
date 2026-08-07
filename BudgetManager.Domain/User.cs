using System;

namespace BudgetManager.Domain
{
    /// <summary>
    /// A registered user of the system. Maps a managed-provider identity (Google's <see cref="Subject"/>)
    /// to a stable internal <see cref="Id"/> that is used as the OwnerUserId for all of that user's data.
    /// Unlike the aggregates, users are global (not per-owner scoped) — this is the directory that other
    /// data is partitioned by.
    /// </summary>
    public class User
    {
        /// <summary>Internal id used as OwnerUserId for this user's data.</summary>
        public Guid Id { get; set; }

        /// <summary>Provider subject claim ("sub"), e.g. Google's numeric account id. May be null on a seed row until first login links it.</summary>
        public string? Subject { get; set; }

        /// <summary>User's email (from the verified token claim), used for display and seed linking.</summary>
        public string? Email { get; set; }

        /// <summary>Display name, if the provider supplies one.</summary>
        public string? Name { get; set; }

        /// <summary>When the user was first provisioned.</summary>
        public DateTime CreatedUtc { get; set; }
    }
}
