using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
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
        int userId = 1; // Exemplo: obter ID do usuário autenticado
        var tema = await _temaService.BuscarPorUsuarioIdAsync(userId);
        var model = new TemaViewModel
        {
            ModoEscuro = tema?.ModoEscuro ?? false,
            CorPrimaria = tema?.CorPrimaria ?? "azul"
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TemaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int userId = 1; // Exemplo: obter ID do usuário autenticado
        var tema = new Tema
        {
            UsuarioId = userId,
            ModoEscuro = model.ModoEscuro,
            CorPrimaria = model.CorPrimaria,
            UsuarioInclusao = "system",
            UsuarioAlteracao = "system"
        };
        await _temaService.SalvarAsync(tema);
        return RedirectToAction("Index", "Home");
    }
}

