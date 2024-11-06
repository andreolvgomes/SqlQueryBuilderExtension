using SqlQueryBuilderExtension;
using System;
using System.Linq.Expressions;

namespace Tests
{
    public class Queries
    {
        public static SqlQueryResult Query<TEntity>(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> selector = null)
        {
            var result = expression.BuildQuery(selector);
            return result;
        }
    }
}