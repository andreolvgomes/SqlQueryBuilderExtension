using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using System.Linq.Expressions;

namespace SqlQueryBuilderExtension
{
    public class QueryPart
    {
        public string Where { get; set; }
        public string Fields { get; set; }
        public string Sql { get; set; }

        public bool HasSql => !string.IsNullOrEmpty(Where);

        public IReadOnlyList<Parameter> Parameters { get; }

        private QueryPart(string sql, params Parameter[] parameters)
            : this(sql, parameters.ToList())
        {
        }

        private QueryPart(string sql, IEnumerable<Parameter> parameters)
        {
            Where = sql;
            Parameters = new List<Parameter>(parameters);
        }

        public static QueryPart IsSql(string sql)
        {
            return new QueryPart(sql);
        }

        public static QueryPart IsParameter(int count, object value)
        {
            return new QueryPart($"@param{count}", new Parameter($"param{count.ToString()}", value));
        }

        public static QueryPart IsCollection(ref int countStart, IEnumerable values)
        {
            var parameters = new List<Parameter>();
            var sql = new StringBuilder("(");
            foreach (var value in values)
            {
                parameters.Add(new Parameter($"param{countStart.ToString()}", value));
                sql.Append($"@param{countStart},");
                countStart++;
            }

            if (sql.Length == 1)
            {
                sql.Append("null,");
            }

            sql[sql.Length - 1] = ')';
            return new QueryPart(sql.ToString(), parameters);
        }

        public static QueryPart Concat(string @operator, QueryPart operand)
        {
            return new QueryPart($"({@operator} {operand.Where})", operand.Parameters);
        }

        public static QueryPart Concat(QueryPart left, string @operator, QueryPart right)
        {
            if (right.Where.Equals("NULL", StringComparison.InvariantCultureIgnoreCase))
            {
                @operator = @operator == "=" ? "IS" : "IS NOT";
            }

            return new QueryPart($"({left.Where} {@operator} {right.Where})", left.Parameters.Union(right.Parameters));
        }

        public static QueryPart Empty => new QueryPart(string.Empty);

        public void ProcessSql<T>(QueryPart wherePart, Expression<Func<T, object>> selector)
        {
            if (selector != null)
            {
                wherePart.Fields = SelectImpl<T>(selector);
                wherePart.Sql = $"select {wherePart.Fields} from dbo.[{typeof(T).Name}]";
            }
            else
            {
                wherePart.Sql = $"select * from dbo.[{typeof(T).Name}]";
            }
            if (!string.IsNullOrEmpty(wherePart.Where))
                wherePart.Sql += " where " + wherePart.Where;
        }

        public string SelectImpl<T>(Expression<Func<T, object>> selector)
        {
            var sb = new StringBuilder();
            var body = (selector.Body as System.Linq.Expressions.NewExpression);
            if (body == null) return "";

            var fields = GetFields(selector);

            int i = 0;
            foreach (var name in fields)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(string.Format("[{0}]", name));
                i++;
            }
            return sb.ToString();
        }

        private static List<string> GetFields<T>(Expression<Func<T, object>> selector)
        {
            var sb = new StringBuilder();
            var body = (selector.Body as System.Linq.Expressions.NewExpression);
            if (body == null) return new List<string>();

            var fields = body.Members.Select(x => x.Name).ToList();
            return fields;
        }
    }
}