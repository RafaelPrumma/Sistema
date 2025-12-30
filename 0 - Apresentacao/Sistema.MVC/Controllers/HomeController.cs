using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using System.Diagnostics;

namespace Sistema.MVC.Controllers
{
        public class HomeController(ILogger<HomeController> logger, IUsuarioService usuarioService, IPerfilService perfilService, IFuncionalidadeService funcionalidadeService, IConfiguracaoService configuracaoService, IMensagemService mensagemService) : Controller
        {
                private readonly ILogger<HomeController> _logger = logger;
                private readonly IUsuarioService _usuarioService = usuarioService;
                private readonly IPerfilService _perfilService = perfilService;
                private readonly IFuncionalidadeService _funcionalidadeService = funcionalidadeService;
                private readonly IConfiguracaoService _configuracaoService = configuracaoService;
                private readonly IMensagemService _mensagemService = mensagemService;

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
                                var msgs = await _mensagemService.BuscarCaixaEntradaAsync(userId.Value, 1, 1);
                                model.TotalMensagens = msgs.TotalCount;
                        }

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
