using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using System.Linq;
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

        public static QueryPart ToSql<T>(this Expression<Func<T, bool>> expression, Expression<Func<T, object>> selector = null)
        {
            var i = 1;

            var result = Recurse<T>(ref i, expression.Body, isUnary: true);
            result.ProcessSql<T>(result, selector);
            return result;
        }

        private static QueryPart Recurse<T>(ref int i, Expression expression, bool isUnary = false,
            string prefix = null, string postfix = null, bool left = true)
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

        private static QueryPart InvocationExpressionExtract<T>(ref int i, InvocationExpression expression, bool left)
        {
            return Recurse<T>(ref i, ((Expression<Func<T, bool>>)expression.Expression).Body, left: left);
        }

        private static QueryPart MethodCallExpressionExtract<T>(ref int i, MethodCallExpression expression)
        {
            // LIKE queries:
            if (expression.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
            {
                return QueryPart.Concat(Recurse<T>(ref i, expression.Object), "LIKE",
                    Recurse<T>(ref i, expression.Arguments[0], prefix: "%", postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
            {
                return QueryPart.Concat(Recurse<T>(ref i, expression.Object), "LIKE",
                    Recurse<T>(ref i, expression.Arguments[0], postfix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
            {
                return QueryPart.Concat(Recurse<T>(ref i, expression.Object), "LIKE",
                    Recurse<T>(ref i, expression.Arguments[0], prefix: "%"));
            }

            if (expression.Method == typeof(string).GetMethod("Equals", new[] { typeof(string) }))
            {
                return QueryPart.Concat(Recurse<T>(ref i, expression.Object), "=",
                    Recurse<T>(ref i, expression.Arguments[0], left: false));
            }

            // IN queries:
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
                return QueryPart.Concat(Recurse<T>(ref i, property), "IN", QueryPart.IsCollection(ref i, values));
            }

            throw new Exception("Unsupported method call: " + expression.Method.Name);
        }

        private static QueryPart MemberExpressionExtract<T>(ref int i, MemberExpression expression, bool isUnary,
            string prefix, string postfix, bool left)
        {
            if (isUnary && expression.Type == typeof(bool))
            {
                return QueryPart.Concat(Recurse<T>(ref i, expression), "=", QueryPart.IsSql("1"));
            }

            if (expression.Member is PropertyInfo property)
            {
                if (left)
                {
                    var colName = GetName(property);
                    return QueryPart.IsSql($"[{colName}]");
                }

                if (property.PropertyType == typeof(bool))
                {
                    var colName = GetName(property);
                    return QueryPart.IsSql($"[{colName}]=1");
                }
            }

            if (expression.Member is FieldInfo || left == false)
            {
                var value = GetValue(expression);
                if (value is string textValue)
                {
                    value = prefix + textValue + postfix;
                }

                return QueryPart.IsParameter(i++, value);
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

        private static QueryPart ConstantExpressionExtract(ref int i, ConstantExpression expression, bool isUnary,
            string prefix, string postfix, bool left)
        {
            var value = expression.Value;

            switch (value)
            {
                case null:
                    return QueryPart.IsSql("NULL");
                case int _:
                    return QueryPart.IsSql(value.ToString());
                case string text:
                    value = prefix + text + postfix;
                    break;
            }

            if (!(value is bool boolValue) || isUnary) return QueryPart.IsParameter(i++, value);

            string result;
            if (left)
                result = boolValue ? "1=1" : "0=1";
            else
                result = boolValue ? "1" : "0";

            return QueryPart.IsSql(result);
        }

        private static QueryPart BinaryExpressionExtract<T>(ref int i, BinaryExpression expression)
        {
            QueryPart left = null;
            if (expression.Left is MemberExpression memberExpr && memberExpr.Type == typeof(bool) && expression.Right as ConstantExpression == null)
                left = Recurse<T>(ref i, expression.Left, isUnary: true);
            else
                left = Recurse<T>(ref i, expression.Left);

            var right = Recurse<T>(ref i, expression.Right, left: false);
            var oper = NodeTypeToString(expression.NodeType);
            return QueryPart.Concat(left, oper, right);
        }

        private static QueryPart UnaryExpressionExtract<T>(ref int i, UnaryExpression expression)
        {
            return QueryPart.Concat(NodeTypeToString(expression.NodeType), Recurse<T>(ref i, expression.Operand, true));
        }

        private static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static string NodeTypeToString(ExpressionType nodeType)
        {
            return nodeTypeMappings.TryGetValue(nodeType, out var value)
                ? value
                : string.Empty;
        }

        public static List<T> AsList<T>(this IEnumerable<T> source) =>
            (source == null || source is List<T>) ? (List<T>)source : source.ToList();
    }
}