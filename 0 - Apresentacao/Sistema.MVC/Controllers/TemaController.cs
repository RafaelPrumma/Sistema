using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;

namespace Sistema.MVC.Controllers;

public class TemaController : Controller
{
    private readonly ITemaService _temaService;

    public TemaController(ITemaService temaService)
    {
        _temaService = temaService;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
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

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId is null)
            return RedirectToAction("Login", "Account");
        var userName = HttpContext.Session.GetString("UserName") ?? "system";
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

