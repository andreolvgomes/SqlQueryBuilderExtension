using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using System.Linq.Expressions;

namespace SqlQueryBuilderExtension
{
    public class SqlQueryResult
    {
        public string Where { get; set; }
        public string Fields { get; set; } = "*";
        public string Sql { get; set; }

        public bool HasSql => !string.IsNullOrEmpty(Where);
        public static SqlQueryResult Empty => new SqlQueryResult(string.Empty);

        public IReadOnlyList<SqlQueryParameter> Parameters { get; }

        private SqlQueryResult(string sql, params SqlQueryParameter[] parameters)
            : this(sql, parameters.ToList())
        {
        }

        private SqlQueryResult(string sql, IEnumerable<SqlQueryParameter> parameters)
        {
            Where = sql;
            Parameters = new List<SqlQueryParameter>(parameters);
        }

        public static SqlQueryResult IsSql(string sql)
        {
            return new SqlQueryResult(sql);
        }

        public static SqlQueryResult IsParameter(int count, object value)
        {
            return new SqlQueryResult($"@param{count}", new SqlQueryParameter($"param{count.ToString()}", value));
        }

        public static SqlQueryResult IsCollection(ref int countStart, IEnumerable values)
        {
            var parameters = new List<SqlQueryParameter>();
            var sql = new StringBuilder("(");
            foreach (var value in values)
            {
                parameters.Add(new SqlQueryParameter($"param{countStart.ToString()}", value));
                sql.Append($"@param{countStart},");
                countStart++;
            }

            if (sql.Length == 1)
            {
                sql.Append("null,");
            }

            sql[sql.Length - 1] = ')';
            return new SqlQueryResult(sql.ToString(), parameters);
        }

        public static SqlQueryResult Append(string @operator, SqlQueryResult operand)
        {
            return new SqlQueryResult($"({@operator} {operand.Where})", operand.Parameters);
        }

        public static SqlQueryResult Append(SqlQueryResult left, string @operator, SqlQueryResult right)
        {
            if (right.Where.Equals("NULL", StringComparison.InvariantCultureIgnoreCase))
            {
                @operator = @operator == "=" ? "IS" : "IS NOT";
            }

            return new SqlQueryResult($"({left.Where} {@operator} {right.Where})", left.Parameters.Union(right.Parameters));
        }

        public void TreatSql<T>(SqlQueryResult wherePart, Expression<Func<T, object>> selector)
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
            var fields = GetPropertyNames(selector);

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

        public static List<string> GetPropertyNames<T>(Expression<Func<T, object>> expression)
        {
            var propertyNames = new List<string>();

            if (expression.Body is MemberExpression memberExpression)
            {
                // Caso de uma única propriedade, como s => s.Pro_descricao
                propertyNames.Add(memberExpression.Member.Name);
            }
            else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMember)
            {
                // Caso de conversão para object, como em s => (object)s.Pro_descricao
                propertyNames.Add(unaryMember.Member.Name);
            }
            else if (expression.Body is NewExpression newExpression)
            {
                // Caso de múltiplas propriedades, como s => new { s.Pro_codigo, s.Pro_descricao, s.Pro_pvenda }
                foreach (var argument in newExpression.Arguments)
                {
                    if (argument is MemberExpression newMemberExpression)
                    {
                        propertyNames.Add(newMemberExpression.Member.Name);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Expressão não é uma propriedade válida ou uma seleção de múltiplas propriedades.");
            }

            return propertyNames;
        }
    }
}