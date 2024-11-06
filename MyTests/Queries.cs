using System.Linq.Expressions;
using SqlQueryBuilderExtension;

namespace MyTests
{
    public class Queries
    {
        public static QueryPart Query<TEntity>(Expression<Func<TEntity, bool>> expression)
        {
            var result = expression.ToSql();
            return result;
        }
    }
}