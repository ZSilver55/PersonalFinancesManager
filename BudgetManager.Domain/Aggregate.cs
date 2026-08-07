using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BudgetManager.Domain
{
    public abstract class Aggregate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user who owns this record. Enables per-user data isolation once authentication
        /// is added. Guid.Empty is the implicit single local user.
        /// </summary>
        public Guid OwnerUserId { get; set; } = Guid.Empty;
    }
}
