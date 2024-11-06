using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using System;

namespace SqlQueryBuilderExtension
{
    public static class SqlQueryBuilder
    {
        private static readonly IDictionary<ExpressionType, string> nodeTypeMappings = new Dictionary<ExpressionType, string>
        {
            {ExpressionType.Modulo, "%"},
            {ExpressionType.Multiply, "*"},
            {ExpressionType.Negate, "-"},
            {ExpressionType.Add, "+"},
            {ExpressionType.Divide, "/"},
            {ExpressionType.ExclusiveOr, "^"},
            {ExpressionType.Subtract, "-"},
            {ExpressionType.And, "and"},
            {ExpressionType.AndAlso, "and"},
            {ExpressionType.Equal, "="},
            {ExpressionType.GreaterThan, ">"},
            {ExpressionType.GreaterThanOrEqual, ">="},
            {ExpressionType.LessThan, "<"},
            {ExpressionType.LessThanOrEqual, "<="},
            {ExpressionType.Not, "not"},
            {ExpressionType.NotEqual, "<>"},
            {ExpressionType.Or, "or"},
            {ExpressionType.OrElse, "or"},
        };

        public static SqlQueryResult BuildQuery<T>(this Expression<Func<T, bool>> expression, Expression<Func<T, object>> selector = null)
        {
            var i = 1;

            var result = Expression<T>(ref i, expression.Body, isUnary: true);
            result.TreatSql<T>(result, selector);
            return result;
        }

        private static SqlQueryResult Expression<T>(ref int i, Expression expression, bool isUnary = false, string prefix = null, string postfix = null, bool left = true)
        {
            switch (expression)
            {
                case UnaryExpression unary: return UnaryExpressionExtract<T>(ref i, unary);
                case BinaryExpression binary: return BinaryExpressionExtract<T>(ref i, binary);
                case ConstantExpression constant: return ConstantExpressionExtract(ref i, constant, isUnary, prefix, postfix, left);
                case MemberExpression member: return MemberExpressionExtract<T>(ref i, member, isUnary, prefix, postfix, left);
                case MethodCallExpression method: return MethodCallExpressionExtract<T>(ref i, method);
                case InvocationExpression invocation: return InvocationExpressionExtract<T>(ref i, invocation, left);
                default: throw new Exception("Unsupported expression: " + expression.GetType().Name);
            }
        }

        private static SqlQueryResult InvocationExpressionExtract<T>(ref int i, InvocationExpression expression, bool left)
        {
            return Expression<T>(ref i, ((Expression<Func<T, bool>>)expression.Expression).Body, left: left);
        }

        private static SqlQueryResult MethodCallExpressionExtract<T>(ref int i, MethodCallExpression expression)
        {
            // LIKE queries:
            if (expression.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
            {
                return SqlQueryResult.Append(Expression<T>(ref i, expression.Object), "LIKE",
                    Expression<T>(ref i, expression.Arguments[0], prefix: "%", postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
            {
                return SqlQueryResult.Append(Expression<T>(ref i, expression.Object), "LIKE",
                    Expression<T>(ref i, expression.Arguments[0], postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
            {
                return SqlQueryResult.Append(Expression<T>(ref i, expression.Object), "LIKE",
                    Expression<T>(ref i, expression.Arguments[0], prefix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("Equals", new[] { typeof(string) }))
            {
                return SqlQueryResult.Append(Expression<T>(ref i, expression.Object), "=",
                    Expression<T>(ref i, expression.Arguments[0], left: false));
            }

            if (expression.Method.Name == "Contains")
            {
                Expression collection;
                Expression property;
                if (expression.Method.IsDefined(typeof(ExtensionAttribute)) && expression.Arguments.Count == 2)
                {
                    collection = expression.Arguments[0];
                    property = expression.Arguments[1];
                }
                else if (!expression.Method.IsDefined(typeof(ExtensionAttribute)) && expression.Arguments.Count == 1)
                {
                    collection = expression.Object;
                    property = expression.Arguments[0];
                }
                else
                {
                    throw new Exception("Unsupported method call: " + expression.Method.Name);
                }

                var values = (IEnumerable)GetValue(collection);
                return SqlQueryResult.Append(Expression<T>(ref i, property), "IN", SqlQueryResult.IsCollection(ref i, values));
            }

            throw new Exception("Unsupported method call: " + expression.Method.Name);
        }

        private static SqlQueryResult MemberExpressionExtract<T>(ref int i, MemberExpression expression, bool isUnary, string prefix, string postfix, bool left)
        {
            if (isUnary && expression.Type == typeof(bool))
            {
                return SqlQueryResult.Append(Expression<T>(ref i, expression), "=", SqlQueryResult.IsSql("1"));
            }

            if (expression.Member is PropertyInfo property)
            {
                if (left)
                {
                    var colName = GetName(property);
                    return SqlQueryResult.IsSql($"[{colName}]");
                }

                if (property.PropertyType == typeof(bool))
                {
                    var colName = GetName(property);
                    return SqlQueryResult.IsSql($"[{colName}]=1");
                }
            }

            if (expression.Member is FieldInfo || left == false)
            {
                var value = GetValue(expression);
                if (value is string textValue)
                {
                    value = prefix + textValue + postfix;
                }

                return SqlQueryResult.IsParameter(i++, value);
            }

            throw new Exception($"Expression does not refer to a property or field: {expression}");
        }

        private static string GetName(PropertyInfo pi)
        {
            return pi.Name;
        }

        private static string GetName(Type type)
        {
            return type.Name;
        }

        private static SqlQueryResult ConstantExpressionExtract(ref int i, ConstantExpression expression, bool isUnary, string prefix, string postfix, bool left)
        {
            var value = expression.Value;

            switch (value)
            {
                case null:
                    return SqlQueryResult.IsSql("NULL");
                case int _:
                    return SqlQueryResult.IsSql(value.ToString());
                case string text:
                    value = prefix + text + postfix;
                    break;
            }

            if (!(value is bool boolValue) || isUnary) return SqlQueryResult.IsParameter(i++, value);

            string result;
            if (left)
                result = boolValue ? "1=1" : "0=1";
            else
                result = boolValue ? "1" : "0";

            return SqlQueryResult.IsSql(result);
        }

        private static SqlQueryResult BinaryExpressionExtract<T>(ref int i, BinaryExpression expression)
        {
            SqlQueryResult left = null;

            // trata boleano, exemplo: (s => s.Ativo)
            // sem o == True explicito
            if (expression.Left is MemberExpression memberExpr && memberExpr.Type == typeof(bool) && expression.Right as ConstantExpression == null)
                left = Expression<T>(ref i, expression.Left, isUnary: true);
            else
                left = Expression<T>(ref i, expression.Left);

            var right = Expression<T>(ref i, expression.Right, left: false);
            var oper = NodeTypeToString(expression.NodeType);

            return SqlQueryResult.Append(left, oper, right);
        }

        private static SqlQueryResult UnaryExpressionExtract<T>(ref int i, UnaryExpression expression)
        {
            return SqlQueryResult.Append(NodeTypeToString(expression.NodeType), Expression<T>(ref i, expression.Operand, true));
        }

        private static object GetValue(Expression member)
        {
            var objectMember = System.Linq.Expressions.Expression.Convert(member, typeof(object));
            var getterLambda = System.Linq.Expressions.Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static string NodeTypeToString(ExpressionType nodeType)
        {
            return nodeTypeMappings.TryGetValue(nodeType, out var value)
                ? value
                : string.Empty;
        }
    }
}