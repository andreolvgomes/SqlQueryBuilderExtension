using MyTests;
using MyTests.Tests;
using System.Reflection;

//var produto = new Produtos() { Pro_codigo = "teste" };

//Queries.Query<Produtos>(s => s.Pro_pvenda - 10 == 10);
//Queries.Query<Produtos>(s => -s.Pro_pvenda - 10 == 10);
//Queries.Query<Produtos>(s => s.Dt == produto.Dt);
//Queries.Query<Produtos>(s => s.Pro_ativ && s.Pro_codigo == "123456");
//Queries.Query<Produtos>(s => s.Pro_ativ);
//Queries.Query<Produtos>(s => !s.Pro_ativ);
//Queries.Query<Produtos>(s => s.Pro_ativ == true);
//Queries.Query<Produtos>(s => !s.Pro_ativ);
//Queries.Query<Produtos>(s => s.Pro_ativ == true && s.Pro_codigo == "123456");
//Queries.Query<Produtos>(s => s.Pro_ativ && s.Pro_codigo == produto.Pro_codigo);

//Queries.Query<Produtos>(s => (s.Pro_ativ || s.Pro_codigo == produto.Pro_codigo) && s.Pro_pvenda > 0 || s.Pro_codigo == "123");

var tests = new SqlQueryBuilderTests();
var methods = tests.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
foreach (var method in methods)
{
    if (method.DeclaringType == typeof(SqlQueryBuilderTests) && method.GetParameters().Length == 0)
        method.Invoke(tests, null);
}

Console.WriteLine("Sucesso!!!");