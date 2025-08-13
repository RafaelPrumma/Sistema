using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.MVC.Models;

namespace Sistema.MVC.Controllers;

public class LayoutController : Controller
{
    private readonly ILayoutService _layoutService;

    public LayoutController(ILayoutService layoutService)
    {
        _layoutService = layoutService;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        int userId = 1; // Exemplo: obter ID do usuário autenticado
        var layout = await _layoutService.BuscarPorUsuarioIdAsync(userId);
        var model = new LayoutViewModel
        {
            ModoEscuro = layout?.ModoEscuro ?? false,
            CorPrimaria = layout?.CorPrimaria ?? "azul"
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(LayoutViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        int userId = 1; // Exemplo: obter ID do usuário autenticado
        var layout = new Layout
        {
            UsuarioId = userId,
            ModoEscuro = model.ModoEscuro,
            CorPrimaria = model.CorPrimaria,
            UsuarioInclusao = "system",
            UsuarioAlteracao = "system"
        };
        await _layoutService.SalvarAsync(layout);
        return RedirectToAction("Index", "Home");
    }
}

