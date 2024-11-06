using System.Data;

namespace SqlQueryBuilderExtension
{
    public class SqlQueryParameter
    {
        public SqlQueryParameter(string key, object value, DbType? type = null)
        {
            Key = key;
            Value = value;
            Type = type ?? GetValueType(value);
        }

        public string Key { get; }
        public object Value { get; }
        public DbType? Type { get; }

        private static DbType? GetValueType(object value)
        {
            if (value is string)
            {
                return DbType.AnsiString;
            }
            
            return null; // Use SqlMapper DefaultTypes mapping
        }
    }
}