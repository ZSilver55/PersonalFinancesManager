using BudgetManager.Domain.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Domain
{
    public class Category : Aggregate
    {
        [Required]
        public string Name { get; set; }

        public CategoryType? Type { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public string Color { get; set; }

        public string Icon { get; set; }
    }
}
