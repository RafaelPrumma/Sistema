using Moq;
using Microsoft.EntityFrameworkCore;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;
using Sistema.INFRA.Repositories;

namespace Sistema.Tests;

/// <summary>
/// Testa o ajuste de cotas por evento corporativo em BuscarTodasTransacoesAsync.
/// Regra (spec §5): para transação com Date &lt; Evento.Data, Quantity *= Fator e UnitPrice /= Fator.
/// GrossAmount permanece inalterado.
/// </summary>
public class EventoCorporativoTests
{
    // --- Cenário principal: compra pré-split + venda pós-split ---

    [Fact]
    public async Task CompraPréSplitEVendaPósSplit_SaldoEPrecoMedioCorretos()
    {
        // BCFF11 split 1:8 em 28/11/2023.
        // Compra pré-split: 100 cotas a R$80 → R$8.000.
        //   Após ajuste: 800 cotas a R$10.
        // Venda pós-split: 200 cotas a R$10 → R$2.000.
        // Saldo esperado: 600 cotas; PM esperado: R$10.

        await using var ctx = CriarContexto();

        var ativo = new AtivoFinanceiro
        {
            Id = 1, AssetKey = "BCFF11", Ticker = "BCFF11",
            Name = "FII BC FFII CI", AssetClass = ClasseAtivo.FII,
            Market = "B3", UsuarioInclusao = "test"
        };
        ctx.AtivosFinanceiros.Add(ativo);

        var splitData = new DateTime(2023, 11, 28);

        // Transação pré-split (data antes do evento).
        var compraPre = new TransacaoFinanceira
        {
            AssetId = 1, Asset = ativo,
            Date = new DateTime(2023, 6, 15),
            OperationType = TipoOperacaoFinanceira.Compra,
            Quantity = 100m,       // cotas pré-split
            UnitPrice = 80m,       // preço pré-split
            GrossAmount = 8000m,
            IsCanonical = true, RawJson = "{}", UsuarioInclusao = "test"
        };
        // Transação pós-split (data igual ou após o evento).
        var vendaPos = new TransacaoFinanceira
        {
            AssetId = 1, Asset = ativo,
            Date = new DateTime(2024, 1, 10),
            OperationType = TipoOperacaoFinanceira.Venda,
            Quantity = 200m,       // cotas pós-split
            UnitPrice = 10m,       // preço pós-split
            GrossAmount = 2000m,
            IsCanonical = true, RawJson = "{}", UsuarioInclusao = "test"
        };
        ctx.TransacoesFinanceiras.AddRange(compraPre, vendaPos);

        var evento = new EventoCorporativo
        {
            AtivoFinanceiroId = 1, AtivoFinanceiro = ativo,
            Tipo = TipoEventoCorporativo.Desdobramento,
            Data = splitData, Fator = 8m,
            Fonte = "Teste", ChaveNatural = "BCFF11|20231128|8",
            UsuarioInclusao = "test"
        };
        ctx.EventosCorporativos.Add(evento);
        await ctx.SaveChangesAsync();

        var repo = new FinancasRepository(ctx);
        var transacoes = await repo.BuscarTodasTransacoesAsync();

        // A compra pré-split deve ter sido ajustada para 800 cotas a R$10.
        var compraAjustada = transacoes.Single(t => t.OperationType == TipoOperacaoFinanceira.Compra);
        Assert.Equal(800m, compraAjustada.Quantity);
        Assert.Equal(10m, compraAjustada.UnitPrice);
        Assert.Equal(8000m, compraAjustada.GrossAmount); // GrossAmount não muda.

        // A venda pós-split não deve ser alterada.
        var vendaNaoAjustada = transacoes.Single(t => t.OperationType == TipoOperacaoFinanceira.Venda);
        Assert.Equal(200m, vendaNaoAjustada.Quantity);
        Assert.Equal(10m, vendaNaoAjustada.UnitPrice);

        // Posição calculada: compra 800 - venda 200 = 600 cotas.
        // PM = total investido / quantidade = 8000 / 800 = R$10.
        var quantidadeComprada = compraAjustada.Quantity;
        var quantidadeVendida = vendaNaoAjustada.Quantity;
        var saldo = quantidadeComprada - quantidadeVendida;
        Assert.Equal(600m, saldo);

        var pmCalculado = compraAjustada.GrossAmount / compraAjustada.Quantity;
        Assert.Equal(10m, pmCalculado);
    }

    [Fact]
    public async Task TransacaoPósSplit_NaoEhAjustada()
    {
        // Transação com data igual ou posterior ao evento não deve ser ajustada.
        await using var ctx = CriarContexto();

        var ativo = new AtivoFinanceiro
        {
            Id = 1, AssetKey = "GGRC11", Ticker = "GGRC11",
            Name = "FII GGR COVEPI CI", AssetClass = ClasseAtivo.FII,
            Market = "B3", UsuarioInclusao = "test"
        };
        ctx.AtivosFinanceiros.Add(ativo);

        var compraPosDate = new TransacaoFinanceira
        {
            AssetId = 1, Asset = ativo,
            Date = new DateTime(2024, 3, 6), // exatamente na data do split → pós-split
            OperationType = TipoOperacaoFinanceira.Compra,
            Quantity = 100m, UnitPrice = 9m, GrossAmount = 900m,
            IsCanonical = true, RawJson = "{}", UsuarioInclusao = "test"
        };
        ctx.TransacoesFinanceiras.Add(compraPosDate);

        ctx.EventosCorporativos.Add(new EventoCorporativo
        {
            AtivoFinanceiroId = 1, AtivoFinanceiro = ativo,
            Tipo = TipoEventoCorporativo.Desdobramento,
            Data = new DateTime(2024, 3, 6), Fator = 10m,
            Fonte = "Teste", ChaveNatural = "GGRC11|20240306|10",
            UsuarioInclusao = "test"
        });
        await ctx.SaveChangesAsync();

        var repo = new FinancasRepository(ctx);
        var transacoes = await repo.BuscarTodasTransacoesAsync();

        var compra = transacoes.Single();
        // Date não é < Data do evento (é igual), portanto não deve ser ajustada.
        Assert.Equal(100m, compra.Quantity);
        Assert.Equal(9m, compra.UnitPrice);
    }

    [Fact]
    public async Task DoisEventosNoMesmoAtivo_ProdutoDossFatoresEAplicado()
    {
        // Cenário hipotético: dois splits em momentos diferentes.
        // Compra antes dos dois → fator acumulado = f1 × f2.
        await using var ctx = CriarContexto();

        var ativo = new AtivoFinanceiro
        {
            Id = 1, AssetKey = "TST11", Ticker = "TST11",
            Name = "FII TESTE", AssetClass = ClasseAtivo.FII,
            Market = "B3", UsuarioInclusao = "test"
        };
        ctx.AtivosFinanceiros.Add(ativo);

        var compra = new TransacaoFinanceira
        {
            AssetId = 1, Asset = ativo,
            Date = new DateTime(2022, 1, 1),
            OperationType = TipoOperacaoFinanceira.Compra,
            Quantity = 10m, UnitPrice = 40m, GrossAmount = 400m,
            IsCanonical = true, RawJson = "{}", UsuarioInclusao = "test"
        };
        ctx.TransacoesFinanceiras.Add(compra);

        ctx.EventosCorporativos.AddRange(
            new EventoCorporativo
            {
                AtivoFinanceiroId = 1, AtivoFinanceiro = ativo,
                Tipo = TipoEventoCorporativo.Desdobramento,
                Data = new DateTime(2022, 6, 1), Fator = 2m,
                Fonte = "Teste", ChaveNatural = "TST11|20220601|2",
                UsuarioInclusao = "test"
            },
            new EventoCorporativo
            {
                AtivoFinanceiroId = 1, AtivoFinanceiro = ativo,
                Tipo = TipoEventoCorporativo.Desdobramento,
                Data = new DateTime(2023, 3, 1), Fator = 4m,
                Fonte = "Teste", ChaveNatural = "TST11|20230301|4",
                UsuarioInclusao = "test"
            });
        await ctx.SaveChangesAsync();

        var repo = new FinancasRepository(ctx);
        var transacoes = await repo.BuscarTodasTransacoesAsync();

        var c = transacoes.Single();
        // Fator acumulado = 2 × 4 = 8 → 10 * 8 = 80 cotas; 40 / 8 = R$5
        Assert.Equal(80m, c.Quantity);
        Assert.Equal(5m, c.UnitPrice);
        Assert.Equal(400m, c.GrossAmount); // GrossAmount inalterado
    }

    [Fact]
    public async Task AtivoSemEvento_NaoEhAfetado()
    {
        await using var ctx = CriarContexto();

        var ativo = new AtivoFinanceiro
        {
            Id = 1, AssetKey = "ITUB4", Ticker = "ITUB4",
            Name = "Itau PN", AssetClass = ClasseAtivo.Acao,
            Market = "B3", UsuarioInclusao = "test"
        };
        ctx.AtivosFinanceiros.Add(ativo);

        ctx.TransacoesFinanceiras.Add(new TransacaoFinanceira
        {
            AssetId = 1, Asset = ativo,
            Date = new DateTime(2023, 1, 10),
            OperationType = TipoOperacaoFinanceira.Compra,
            Quantity = 50m, UnitPrice = 25m, GrossAmount = 1250m,
            IsCanonical = true, RawJson = "{}", UsuarioInclusao = "test"
        });
        await ctx.SaveChangesAsync();

        var repo = new FinancasRepository(ctx);
        var transacoes = await repo.BuscarTodasTransacoesAsync();

        var t = transacoes.Single();
        Assert.Equal(50m, t.Quantity);
        Assert.Equal(25m, t.UnitPrice);
    }

    // --- Helpers ---

    private static AppDbContext CriarContexto()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var exec = new Mock<IExecutionContext>();
        exec.SetupGet(x => x.Usuario).Returns("test");
        return new AppDbContext(opts, exec.Object);
    }
}
