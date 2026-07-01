using Moq;
using Microsoft.EntityFrameworkCore;
using Sistema.APP.DTOs;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Repositories;

namespace Sistema.Tests;

/// <summary>
/// F-G — edição do peso-alvo por ativo-em-carteira (CarteiraAtivoFinanceiro.PesoAlvo).
/// Cobre validação de faixa (0..100), persistência via UnitOfWork e o tratamento de 0/null como "sem alvo".
/// </summary>
public class PesoAlvoTests
{
    [Fact]
    public async Task SalvarPesosAlvo_PersisteValorValido()
    {
        var db = Guid.NewGuid().ToString("N");
        await using var ctx = CriarContexto(db);
        await SemearAsync(ctx);

        var service = CriarService(ctx);
        var input = new SalvarPesosAlvoInput
        {
            Itens =
            [
                new PesoAlvoLinhaInput { CarteiraAtivoId = 1, PesoAlvo = 60m },
                new PesoAlvoLinhaInput { CarteiraAtivoId = 2, PesoAlvo = 40m }
            ]
        };

        var resultado = await service.SalvarPesosAlvoAsync(input);

        Assert.True(resultado.Sucesso);
        await using var verificacao = CriarContexto(db);
        Assert.Equal(60m, verificacao.CarteirasAtivosFinanceiros.Single(x => x.Id == 1).PesoAlvo);
        Assert.Equal(40m, verificacao.CarteirasAtivosFinanceiros.Single(x => x.Id == 2).PesoAlvo);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(150)]
    public async Task SalvarPesosAlvo_RejeitaForaDaFaixa(decimal pesoInvalido)
    {
        var db = Guid.NewGuid().ToString("N");
        await using var ctx = CriarContexto(db);
        await SemearAsync(ctx);

        var service = CriarService(ctx);
        var input = new SalvarPesosAlvoInput
        {
            Itens = [new PesoAlvoLinhaInput { CarteiraAtivoId = 1, PesoAlvo = pesoInvalido }]
        };

        var resultado = await service.SalvarPesosAlvoAsync(input);

        Assert.False(resultado.Sucesso);
        // Nada deve ter sido gravado.
        await using var verificacao = CriarContexto(db);
        Assert.Null(verificacao.CarteirasAtivosFinanceiros.Single(x => x.Id == 1).PesoAlvo);
    }

    [Fact]
    public async Task SalvarPesosAlvo_ZeroOuNulo_LimpaOAlvo()
    {
        var db = Guid.NewGuid().ToString("N");
        await using var ctx = CriarContexto(db);
        await SemearAsync(ctx, pesoInicial1: 25m, pesoInicial2: 30m);

        var service = CriarService(ctx);
        var input = new SalvarPesosAlvoInput
        {
            Itens =
            [
                new PesoAlvoLinhaInput { CarteiraAtivoId = 1, PesoAlvo = 0m },     // 0 = sem alvo
                new PesoAlvoLinhaInput { CarteiraAtivoId = 2, PesoAlvo = null }    // vazio = sem alvo
            ]
        };

        var resultado = await service.SalvarPesosAlvoAsync(input);

        Assert.True(resultado.Sucesso);
        await using var verificacao = CriarContexto(db);
        Assert.Null(verificacao.CarteirasAtivosFinanceiros.Single(x => x.Id == 1).PesoAlvo);
        Assert.Null(verificacao.CarteirasAtivosFinanceiros.Single(x => x.Id == 2).PesoAlvo);
    }

    [Fact]
    public async Task ObterPesosAlvo_AgrupaPorCarteiraEMostraAlvoAtual()
    {
        var db = Guid.NewGuid().ToString("N");
        await using var ctx = CriarContexto(db);
        await SemearAsync(ctx, pesoInicial1: 70m, pesoInicial2: 30m);

        var service = CriarService(ctx);
        var dto = await service.ObterPesosAlvoAsync();

        Assert.Single(dto.Carteiras);
        var carteira = dto.Carteiras[0];
        Assert.Equal("Bancos", carteira.CarteiraNome);
        Assert.Equal(2, carteira.Itens.Count);
        Assert.Equal(100m, carteira.SomaPesoAlvo);
        Assert.Equal(100m, dto.SomaTotalPesoAlvo);
        Assert.Contains(carteira.Itens, i => i.Ticker == "BBAS3" && i.PesoAlvo == 70m);
        Assert.Contains(carteira.Itens, i => i.Ticker == "ITUB4" && i.PesoAlvo == 30m);
    }

    // --- Helpers ---

    private static async Task SemearAsync(AppDbContext ctx, decimal? pesoInicial1 = null, decimal? pesoInicial2 = null)
    {
        var carteira = new CarteiraFinanceira { Id = 1, Nome = "Bancos", Slug = "bancos", Ativo = true, UsuarioInclusao = "test" };
        var bbas3 = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "Banco do Brasil", Classe = ClasseAtivo.Acao, Mercado = "B3", UsuarioInclusao = "test" };
        var itub4 = new AtivoFinanceiro { Id = 2, Chave = "ITUB4", Sigla = "ITUB4", Nome = "Itau", Classe = ClasseAtivo.Acao, Mercado = "B3", UsuarioInclusao = "test" };
        ctx.CarteirasFinanceiras.Add(carteira);
        ctx.AtivosFinanceiros.AddRange(bbas3, itub4);
        ctx.CarteirasAtivosFinanceiros.AddRange(
            new CarteiraAtivoFinanceiro { Id = 1, CarteiraFinanceiraId = 1, AtivoFinanceiroId = 1, PesoAlvo = pesoInicial1, Ativo = true, UsuarioInclusao = "test" },
            new CarteiraAtivoFinanceiro { Id = 2, CarteiraFinanceiraId = 1, AtivoFinanceiroId = 2, PesoAlvo = pesoInicial2, Ativo = true, UsuarioInclusao = "test" });
        await ctx.SaveChangesAsync();
        // Limpa o tracking para que as gravações do serviço partam de entidades carregadas de novo.
        ctx.ChangeTracker.Clear();
    }

    private static FinancasAppService CriarService(AppDbContext ctx)
    {
        var repo = new FinancasRepository(ctx);
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Financas).Returns(repo);
        uow.Setup(x => x.ConfirmarAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => ctx.SaveChangesAsync(ct));

        var importador = new Mock<IFinancasImportador>();
        importador.Setup(x => x.GarantirCargaInicialAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var marketData = new Mock<IFinancasMarketDataService>();
        var projection = new Mock<IPosicaoAtivoProjectionService>();
        projection.Setup(x => x.RecalcularAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var log = new Mock<ILogAppService>();
        var mensagem = new Mock<IMensagemAppService>();
        var execution = new Mock<IExecutionContext>();
        execution.SetupGet(x => x.Usuario).Returns("test");

        return new FinancasAppService(uow.Object, importador.Object, marketData.Object, projection.Object, log.Object, mensagem.Object, execution.Object);
    }

    private static AppDbContext CriarContexto(string dbName)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var exec = new Mock<IExecutionContext>();
        exec.SetupGet(x => x.Usuario).Returns("test");
        return new AppDbContext(opts, exec.Object);
    }
}
