namespace MyTests.Tests
{
    public class SqlQueryBuilderTests
    {        
        public void Like()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_descricao.Contains("CASA"));
            result.Assert("select * from dbo.[Produtos] where ([Pro_descricao] LIKE @param1)");
        }

        public void LikeStartWith()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_descricao.StartsWith("CASA"));
            result.Assert("select * from dbo.[Produtos] where ([Pro_descricao] LIKE @param1)");
        }

        public void LikeEndsWith()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_descricao.EndsWith("CASA"));
            result.Assert("select * from dbo.[Produtos] where ([Pro_descricao] LIKE @param1)");
        }

        public void In()
        {
            var listCodigo = new List<string>() { "10046", "121951", "124025" };
            var result = Queries.Query<Produtos>(pro => listCodigo.Contains(pro.Pro_codigo));
            result.Assert("select * from dbo.[Produtos] where ([Pro_codigo] IN (@param1,@param2,@param3))");
        }

        public void NotIn()
        {
            var listCodigo = new List<string>() { "10046", "121951", "124025" };
            var result = Queries.Query<Produtos>(pro => !listCodigo.Contains(pro.Pro_codigo));
            result.Assert("select * from dbo.[Produtos] where (not ([Pro_codigo] IN (@param1,@param2,@param3)))");
        }

        public void Igual()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda == 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] = @param1)");
        }

        public void Maior()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda > 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] > @param1)");
        }

        public void MaiorOuIgual()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda >= 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] >= @param1)");
        }

        public void Menor()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda < 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] < @param1)");
        }

        public void MenorOuIgual()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda <= 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] <= @param1)");
        }

        public void Or()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda == 0 || pro.Pro_pvenda > 0);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] = @param1) or ([Pro_pvenda] > @param2))");
        }

        public void OrAnd()
        {
            var result = Queries.Query<Produtos>(pro => (pro.Pro_pvenda == 0 || pro.Pro_pvenda > 0) && pro.Pro_ativ == true);
            result.Assert("select * from dbo.[Produtos] where ((([Pro_pvenda] = @param1) or ([Pro_pvenda] > @param2)) and ([Pro_ativ] = 1))");
        }

        public void And()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda == 0 && pro.Pro_ativ == true);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] = @param1) and ([Pro_ativ] = 1))");
        }

        public void Diferente()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda != 0);
            result.Assert("select * from dbo.[Produtos] where ([Pro_pvenda] <> @param1)");
        }

        public void Boleano()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_ativ == true);
            result.Assert("select * from dbo.[Produtos] where ([Pro_ativ] = 1)");
            result = Queries.Query<Produtos>(pro => pro.Pro_ativ);
            result.Assert("select * from dbo.[Produtos] where ([Pro_ativ] = 1)");

            result = Queries.Query<Produtos>(pro => pro.Pro_ativ == false);
            result.Assert("select * from dbo.[Produtos] where ([Pro_ativ] = 0)");
            result = Queries.Query<Produtos>(pro => !pro.Pro_ativ);
            result.Assert("select * from dbo.[Produtos] where (not ([Pro_ativ] = 1))");

            result = Queries.Query<Produtos>(pro => pro.Pro_ativ == true && pro.Pro_codigo == "6464564");
            result.Assert("select * from dbo.[Produtos] where (([Pro_ativ] = 1) and ([Pro_codigo] = @param1))");
            result = Queries.Query<Produtos>(pro => pro.Pro_ativ && pro.Pro_codigo == "6464564");
            result.Assert("select * from dbo.[Produtos] where (([Pro_ativ] = 1) and ([Pro_codigo] = @param1))");

            result = Queries.Query<Produtos>(pro => pro.Pro_ativ == false && pro.Pro_codigo == "6464564");
            result.Assert("select * from dbo.[Produtos] where (([Pro_ativ] = 0) and ([Pro_codigo] = @param1))");
            result = Queries.Query<Produtos>(pro => !pro.Pro_ativ && pro.Pro_codigo == "6464564");
            result.Assert("select * from dbo.[Produtos] where ((not ([Pro_ativ] = 1)) and ([Pro_codigo] = @param1))");
        }

        public void DiferenteEIgual()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda != 0 && pro.Pro_codigo == "123");
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] <> @param1) and ([Pro_codigo] = @param2))");
        }

        public void Invertido()
        {
            var result = Queries.Query<Produtos>(pro => !(pro.Pro_pvenda != 0));
            result.Assert("select * from dbo.[Produtos] where (not ([Pro_pvenda] <> @param1))");
        }

        public void ValorVindoDePropriedade()
        {
            var produtos = new Produtos() { Pro_codigo = "123" };
            var result = Queries.Query<Produtos>(pro => pro.Pro_codigo == produtos.Pro_codigo);
            result.Assert("select * from dbo.[Produtos] where ([Pro_codigo] = @param1)");
        }

        public void OperadoresMatematico()
        {
            var result = Queries.Query<Produtos>(pro => pro.Pro_pvenda * 50 == 10);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] * @param1) = @param2)");

            result = Queries.Query<Produtos>(pro => pro.Pro_pvenda * 50 / 100 == 10);
            result.Assert("select * from dbo.[Produtos] where ((([Pro_pvenda] * @param1) / @param2) = @param3)");

            result = Queries.Query<Produtos>(pro => pro.Pro_pvenda + 50 == 10);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] + @param1) = @param2)");

            result = Queries.Query<Produtos>(pro => pro.Pro_pvenda - 50 == 10);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] - @param1) = @param2)");

            result = Queries.Query<Produtos>(pro => pro.Pro_pvenda % 50 == 10);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] % @param1) = @param2)");

            result = Queries.Query<Produtos>(pro => pro.Pro_pvenda / 1 == 10);
            result.Assert("select * from dbo.[Produtos] where (([Pro_pvenda] / @param1) = @param2)");
        }
    }
}