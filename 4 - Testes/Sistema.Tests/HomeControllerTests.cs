using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.MVC.Controllers;
using Sistema.MVC.Models;

namespace Sistema.Tests;

public class HomeControllerTests
{
    [Fact]
    public async Task IndexDeveMontarDashboardOperacionalComAtalhosEMensagens()
    {
        var logger = new Mock<ILogger<HomeController>>();
        var usuarios = new Mock<IUsuarioAppService>();
        var perfis = new Mock<IPerfilAppService>();
        var funcionalidades = new Mock<IFuncionalidadeAppService>();
        var configuracoes = new Mock<IConfiguracaoAppService>();
        var mensagens = new Mock<IMensagemAppService>();

        usuarios
            .Setup(s => s.BuscarTodosAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Usuario>([], 2, 1, 1));
        perfis
            .Setup(s => s.BuscarTodosAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Perfil>([], 3, 1, 1));
        funcionalidades
            .Setup(s => s.BuscarPaginadasAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Funcionalidade>([], 7, 1, 1));
        configuracoes
            .Setup(s => s.BuscarPorAgrupamentoAsync("AzureAd", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Configuracao(), new Configuracao()]);
        mensagens
            .Setup(s => s.BuscarFeedAsync(10, 1, 5, It.IsAny<FeedFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Mensagem>(
                [
                    new Mensagem
                    {
                        Id = 99,
                        Assunto = "Aviso importante",
                        Tipo = PublicacaoTipo.Aviso,
                        Lida = false,
                        DataInclusao = DateTime.UtcNow,
                        Remetente = new Usuario { Nome = "Rafael" }
                    }
                ],
                1,
                1,
                5));
        mensagens
            .Setup(s => s.ContarNaoLidasAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        httpContext.Session.SetInt32("UserId", 10);

        var controller = new HomeController(
            logger.Object,
            usuarios.Object,
            perfis.Object,
            funcionalidades.Object,
            configuracoes.Object,
            mensagens.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.Equal(2, model.TotalUsuarios);
        Assert.Equal(3, model.TotalPerfis);
        Assert.Equal(7, model.TotalFuncionalidades);
        Assert.Equal(2, model.TotalConfiguracoes);
        Assert.Equal(1, model.TotalMensagens);
        Assert.Equal(4, model.TotalMensagensNaoLidas);
        Assert.Contains(model.Atalhos, a => a.Controller == "Financas");
        Assert.Contains(model.MensagensRecentes, m => m.Id == 99 && !m.Lida);
    }
}
