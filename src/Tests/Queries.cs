using SqlQueryBuilderExtension;
using System;
using System.Linq.Expressions;

namespace Tests
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