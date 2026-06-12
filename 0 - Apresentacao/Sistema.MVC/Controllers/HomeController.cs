using Microsoft.AspNetCore.Mvc;
using Sistema.APP.Services.Interfaces;
using Sistema.APP.DTOs;
using Sistema.MVC.Models;
using System.Diagnostics;

namespace Sistema.MVC.Controllers
{
        public class HomeController(ILogger<HomeController> logger, IUsuarioAppService usuarioService, IPerfilAppService perfilService, IFuncionalidadeAppService funcionalidadeService, IConfiguracaoAppService configuracaoService, IMensagemAppService mensagemService) : Controller
        {
                private readonly ILogger<HomeController> _logger = logger;
                private readonly IUsuarioAppService _usuarioService = usuarioService;
                private readonly IPerfilAppService _perfilService = perfilService;
                private readonly IFuncionalidadeAppService _funcionalidadeService = funcionalidadeService;
                private readonly IConfiguracaoAppService _configuracaoService = configuracaoService;
                private readonly IMensagemAppService _mensagemService = mensagemService;

                [HttpGet("/Home")]
                [HttpGet("/Home/Index")]
		public async Task<IActionResult> Index()
                {
                        var model = new DashboardViewModel();

                        var usuarios = await _usuarioService.BuscarTodosAsync(1, 1);
                        model.TotalUsuarios = usuarios.TotalCount;

                        var perfis = await _perfilService.BuscarTodosAsync(1, 1);
                        model.TotalPerfis = perfis.TotalCount;

                        var funcs = await _funcionalidadeService.BuscarPaginadasAsync(1, 1);
                        model.TotalFuncionalidades = funcs.TotalCount;

                        var configs = await _configuracaoService.BuscarPorAgrupamentoAsync("AzureAd");
                        model.TotalConfiguracoes = configs.Count();

                        var userId = HttpContext.Session.GetInt32("UserId");
                        if (userId is not null)
                        {
                                var msgs = await _mensagemService.BuscarFeedAsync(userId.Value, 1, 5, new FeedFiltroDto());
                                model.TotalMensagens = msgs.TotalCount;
                                model.TotalMensagensNaoLidas = await _mensagemService.ContarNaoLidasAsync(userId.Value);
                                model.MensagensRecentes = [.. msgs.Items.Select(m => new DashboardMensagemViewModel
                                {
                                        Id = m.Id,
                                        Assunto = m.Assunto,
                                        Autor = m.Remetente?.Nome ?? m.Autor?.Nome ?? "Sistema",
                                        Tipo = m.Tipo.ToString(),
                                        Lida = m.Lida,
                                        DataInclusao = m.DataInclusao
                                })];
                        }

                        model.Atalhos =
                        [
                                new DashboardAtalhoViewModel
                                {
                                        Titulo = "Finanças",
                                        Descricao = "Dashboard financeiro, documentos, B3, cripto e alertas.",
                                        Icone = "bi-graph-up-arrow",
                                        Controller = "Financas",
                                        Action = "Index",
                                        Variante = "success"
                                },
                                new DashboardAtalhoViewModel
                                {
                                        Titulo = "Comunicacao",
                                        Descricao = "Feed interno, avisos, threads e mensagens diretas.",
                                        Icone = "bi-chat-square-text",
                                        Controller = "Mensagem",
                                        Action = "Index",
                                        Variante = "primary"
                                },
                                new DashboardAtalhoViewModel
                                {
                                        Titulo = "Configuracoes",
                                        Descricao = "Parametros operacionais, email, logs e modulos.",
                                        Icone = "bi-sliders",
                                        Controller = "Configuracao",
                                        Action = "Index",
                                        Variante = "warning"
                                },
                                new DashboardAtalhoViewModel
                                {
                                        Titulo = "Documentacao",
                                        Descricao = "Arquitetura, modulos, seguranca e rotinas operacionais.",
                                        Icone = "bi-journal-code",
                                        Controller = "Documentacao",
                                        Action = "Index",
                                        Variante = "info"
                                }
                        ];

                        return View(model);
                }

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
