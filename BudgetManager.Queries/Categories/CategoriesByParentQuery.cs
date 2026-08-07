using BudgetManager.Domain;
using BudgetManager.Queries.Common;

namespace BudgetManager.Queries.Categories
{
    /// <summary>
    /// Returns the child categories of a given parent. Pass null to get the
    /// top-level categories (those without a parent).
    /// </summary>
    public class CategoriesByParentQuery : JsonQuery<Category>, IQuery<IEnumerable<Category>, Guid?>
    {
        public CategoriesByParentQuery(IJsonStore<Category> store) : base(store) { }

        public async Task<QueryResult<IEnumerable<Category>>> ExecuteQueryAsync(Guid? parentCategoryId)
        {
            var matches = new List<Category>();
            await foreach (var c in Store.StreamAllAsync())
            {
                if (c.ParentCategoryId == parentCategoryId)
                    matches.Add(c);
            }

            matches.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return QueryResult<IEnumerable<Category>>.OK(matches);
        }
    }
}
