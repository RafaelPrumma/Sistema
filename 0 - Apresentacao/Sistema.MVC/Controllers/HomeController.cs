using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using System.Diagnostics;
using System.Linq;

namespace Sistema.MVC.Controllers
{
        public class HomeController : Controller
        {
                private readonly ILogger<HomeController> _logger;
                private readonly IUsuarioService _usuarioService;
                private readonly IPerfilService _perfilService;
                private readonly IFuncionalidadeService _funcionalidadeService;
                private readonly IConfiguracaoService _configuracaoService;
                private readonly IMensagemService _mensagemService;

                public HomeController(
                        ILogger<HomeController> logger,
                        IUsuarioService usuarioService,
                        IPerfilService perfilService,
                        IFuncionalidadeService funcionalidadeService,
                        IConfiguracaoService configuracaoService,
                        IMensagemService mensagemService)
                {
                        _logger = logger;
                        _usuarioService = usuarioService;
                        _perfilService = perfilService;
                        _funcionalidadeService = funcionalidadeService;
                        _configuracaoService = configuracaoService;
                        _mensagemService = mensagemService;
                }

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
