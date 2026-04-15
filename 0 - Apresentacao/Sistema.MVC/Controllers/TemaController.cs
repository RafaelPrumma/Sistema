using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.APP.Services.Interfaces;
using Sistema.MVC.Models;
using System.Security.Claims;
using System.Linq;

namespace Sistema.MVC.Controllers;

public class TemaController(ITemaAppService temaService, ILogger<TemaController> logger) : Controller
{
    private readonly ITemaAppService _temaService = temaService;
    private readonly ILogger<TemaController> _logger = logger;

    private bool EhRequisicaoAjax()
    {
        if (Request.Headers.TryGetValue("X-Requested-With", out var header) && header == "XMLHttpRequest")
            return true;

        return Request.Headers.TryGetValue("Accept", out var accepts)
            && accepts.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }

    private int? ObterUsuarioId()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
            return userId.Value;

        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimId, out var parsedId))
        {
            HttpContext.Session.SetInt32("UserId", parsedId);
            return parsedId;
        }

        return null;
    }

    private string ObterUsuarioNome()
    {
        var userName = HttpContext.Session.GetString("UserName");
        if (!string.IsNullOrWhiteSpace(userName))
            return userName;

        var claimName = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(claimName))
        {
            HttpContext.Session.SetString("UserName", claimName);
            return claimName;
        }

        return "system";
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = ObterUsuarioId();
        if (userId is null)
            return RedirectToAction("Login", "Account");
        var tema = await _temaService.BuscarPorUsuarioIdAsync(userId.Value);
        var model = new TemaViewModel
        {
            ModoEscuro = tema?.ModoEscuro ?? false,
            CorHeader = tema?.CorHeader ?? "#0d6efd",
            CorBarraEsquerda = tema?.CorBarraEsquerda ?? "#0d6efd",
            CorBarraDireita = tema?.CorBarraDireita ?? "#f8f9fa",
            CorFooter = tema?.CorFooter ?? "#0d6efd",
            HeaderFixo = tema?.HeaderFixo ?? false,
            FooterFixo = tema?.FooterFixo ?? false,
            MenuLateralExpandido = tema?.MenuLateralExpandido ?? true
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TemaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (EhRequisicaoAjax())
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(e => !string.IsNullOrWhiteSpace(e));
                return BadRequest(new { success = false, errors });
            }

            return View(model);
        }

        var userId = ObterUsuarioId();
        if (userId is null)
            return RedirectToAction("Login", "Account");
        var userName = ObterUsuarioNome();
        var tema = new Tema
        {
            UsuarioId = userId.Value,
            ModoEscuro = model.ModoEscuro,
            CorHeader = model.CorHeader,
            CorBarraEsquerda = model.CorBarraEsquerda,
            CorBarraDireita = model.CorBarraDireita,
            CorFooter = model.CorFooter,
            HeaderFixo = model.HeaderFixo,
            FooterFixo = model.FooterFixo,
            MenuLateralExpandido = model.MenuLateralExpandido,
            UsuarioInclusao = userName,
            UsuarioAlteracao = userName
        };

        try
        {
            await _temaService.SalvarAsync(tema);
            _logger.LogInformation("Tema atualizado para o usuário {UserId}. HeaderFixo={HeaderFixo}, FooterFixo={FooterFixo}, MenuExpandido={MenuExpandido}, ModoEscuro={ModoEscuro}",
                tema.UsuarioId, tema.HeaderFixo, tema.FooterFixo, tema.MenuLateralExpandido, tema.ModoEscuro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar tema para o usuário {UserId}", tema.UsuarioId);

            if (EhRequisicaoAjax())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    errors = new[] { "Não foi possível salvar o tema neste momento." }
                });
            }

            ModelState.AddModelError(string.Empty, "Não foi possível salvar o tema neste momento.");
            return View(model);
        }

        if (EhRequisicaoAjax())
        {
            return Json(new
            {
                success = true,
                theme = new
                {
                    modoEscuro = tema.ModoEscuro,
                    corHeader = tema.CorHeader,
                    corBarraEsquerda = tema.CorBarraEsquerda,
                    corBarraDireita = tema.CorBarraDireita,
                    corFooter = tema.CorFooter,
                    headerFixo = tema.HeaderFixo,
                    footerFixo = tema.FooterFixo,
                    menuLateralExpandido = tema.MenuLateralExpandido
                }
            });
        }

        return RedirectToAction("Index", "Home");
    }
}
