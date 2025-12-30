using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using System.Security.Claims;

namespace Sistema.MVC.Controllers;

public class TemaController(ITemaService temaService) : Controller
{
    private readonly ITemaService _temaService = temaService;

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
            return View(model);

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
        await _temaService.SalvarAsync(tema);
        return RedirectToAction("Index", "Home");
    }
}

