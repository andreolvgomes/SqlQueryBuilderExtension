using SqlQueryBuilderExtension;

namespace MyTests.Tests
{
    public static class BuildQueryResultExtensions
    {
        public static void Assert(this QueryPart result, string query)
        {
            if (!result.Sql.Equals(query, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Erro, teste não passou");
        }
    }
}