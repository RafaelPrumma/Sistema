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
        var operacional = new FinancasOperacionalDto([], []);

        service.Setup(x => x.ObterPatrimonioDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patrimonio);
        service.Setup(x => x.ObterCarteirasDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(carteiras);
        service.Setup(x => x.ObterImportacaoDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(importacao);
        service.Setup(x => x.ObterOperacionalDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(operacional);
        var controller = new FinancasController(service.Object);

        var patrimonioResult = Assert.IsType<JsonResult>(await controller.DashboardPatrimonio(CancellationToken.None));
        var carteirasResult = Assert.IsType<PartialViewResult>(await controller.DashboardCarteiras(CancellationToken.None));
        var carteirasModel = carteirasResult.Model;
        var importacaoResult = Assert.IsType<PartialViewResult>(await controller.DashboardImportacao(CancellationToken.None));
        var importacaoModel = importacaoResult.Model;
        var operacionalResult = Assert.IsType<PartialViewResult>(await controller.DashboardOperacional(CancellationToken.None));
        var operacionalModel = operacionalResult.Model;

        Assert.Same(patrimonio, patrimonioResult.Value);
        Assert.Equal("_DashboardCarteiras", carteirasResult.ViewName);
        Assert.Same(carteiras, carteirasModel);
        Assert.Equal("_DashboardImportacao", importacaoResult.ViewName);
        Assert.Same(importacao, importacaoModel);
        Assert.Equal("_DashboardOperacional", operacionalResult.ViewName);
        Assert.Same(operacional, operacionalModel);
    }
}
