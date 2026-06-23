using Microsoft.AspNetCore.Mvc;
using Moq;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.MVC.Controllers;

namespace Sistema.Tests;

public class FinancasControllerTests
{
    [Fact]
    public void IndexDeveRetornarShellSemCarregarDashboardCompleto()
    {
        var service = new Mock<IFinancasAppService>(MockBehavior.Strict);
        var controller = new FinancasController(service.Object);

        var result = controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.Model);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepararDashboardDeveExecutarPreparacaoUmaVez()
    {
        var service = new Mock<IFinancasAppService>();
        service.Setup(x => x.PrepararDashboardAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var controller = new FinancasController(service.Object);

        var result = await controller.PrepararDashboard(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(x => x.PrepararDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndpointsDasIlhasDevemRetornarResultadosIndependentes()
    {
        var service = new Mock<IFinancasAppService>();
        var evolucao = new EvolucaoPatrimonioDto([], [], 0, 0, [], []);
        var patrimonio = new FinancasPatrimonioDto(10, 8, 2, evolucao);
        var carteiras = new FinancasCarteirasDto([]);
        var importacao = new FinancasImportacaoDto(
            [],
            new ImportacaoFinanceiraResumoDto(null, 0, 0, 0, null),
            null);
        IReadOnlyList<PosicaoFinanceiraDto> posicoes = [];
        IReadOnlyList<AlertaConfiabilidadeDto> alertas = [];
        var proventos = new FinancasProventosDashboardDto(new ProventosResumoDto(0, 0, 0, 0, 0), [], [], []);

        service.Setup(x => x.ObterPatrimonioDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patrimonio);
        service.Setup(x => x.ObterCarteirasDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(carteiras);
        service.Setup(x => x.ObterImportacaoDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(importacao);
        service.Setup(x => x.ObterPosicoesDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(posicoes);
        service.Setup(x => x.ObterAlertasDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(alertas);
        service.Setup(x => x.ObterProventosDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(proventos);
        var controller = new FinancasController(service.Object);

        var patrimonioResult = Assert.IsType<JsonResult>(await controller.DashboardPatrimonio(CancellationToken.None));
        var carteirasResult = Assert.IsType<PartialViewResult>(await controller.DashboardCarteiras(CancellationToken.None));
        var carteirasModel = carteirasResult.Model;
        var importacaoResult = Assert.IsType<PartialViewResult>(await controller.DashboardImportacao(CancellationToken.None));
        var importacaoModel = importacaoResult.Model;
        var posicoesResult = Assert.IsType<PartialViewResult>(await controller.DashboardPosicoes(CancellationToken.None));
        var posicoesModel = posicoesResult.Model;
        var alertasResult = Assert.IsType<PartialViewResult>(await controller.DashboardAlertas(CancellationToken.None));
        var alertasModel = alertasResult.Model;
        var proventosResult = Assert.IsType<PartialViewResult>(await controller.DashboardProventos(CancellationToken.None));
        var proventosModel = proventosResult.Model;

        Assert.Same(patrimonio, patrimonioResult.Value);
        Assert.Equal("_DashboardCarteiras", carteirasResult.ViewName);
        Assert.Same(carteiras, carteirasModel);
        Assert.Equal("_DashboardImportacao", importacaoResult.ViewName);
        Assert.Same(importacao, importacaoModel);
        Assert.Equal("_DashboardPosicoes", posicoesResult.ViewName);
        Assert.Same(posicoes, posicoesModel);
        Assert.Equal("_DashboardAlertas", alertasResult.ViewName);
        Assert.Same(alertas, alertasModel);
        Assert.Equal("_DashboardProventos", proventosResult.ViewName);
        Assert.Same(proventos, proventosModel);
    }

    [Fact]
    public async Task DashboardReconciliacaoDeveRetornarParcialDaIlha()
    {
        var service = new Mock<IFinancasAppService>();
        var reconciliacao = new FinancasReconciliacaoDto(false, 0, 0, 0, 0, []);
        service.Setup(x => x.ObterReconciliacaoDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(reconciliacao);
        var controller = new FinancasController(service.Object);

        var result = Assert.IsType<PartialViewResult>(await controller.DashboardReconciliacao(CancellationToken.None));

        Assert.Equal("_DashboardReconciliacao", result.ViewName);
        Assert.Same(reconciliacao, result.Model);
        service.Verify(x => x.ObterReconciliacaoDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DashboardProventosDeveRetornarParcialDaIlha()
    {
        var service = new Mock<IFinancasAppService>();
        var proventos = new FinancasProventosDashboardDto(new ProventosResumoDto(0, 0, 0, 0, 0), [], [], []);
        service.Setup(x => x.ObterProventosDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(proventos);
        var controller = new FinancasController(service.Object);

        var result = Assert.IsType<PartialViewResult>(await controller.DashboardProventos(CancellationToken.None));

        Assert.Equal("_DashboardProventos", result.ViewName);
        Assert.Same(proventos, result.Model);
        service.Verify(x => x.ObterProventosDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
