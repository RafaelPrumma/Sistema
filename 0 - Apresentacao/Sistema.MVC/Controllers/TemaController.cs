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
            CorHeader = tema?.CorHeader ?? "#0d6efd",
            CorBarraEsquerda = tema?.CorBarraEsquerda ?? "#0d6efd",
            CorBarraDireita = tema?.CorBarraDireita ?? "#f8f9fa",
            CorFooter = tema?.CorFooter ?? "#0d6efd"
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
            CorHeader = model.CorHeader,
            CorBarraEsquerda = model.CorBarraEsquerda,
            CorBarraDireita = model.CorBarraDireita,
            CorFooter = model.CorFooter,
            UsuarioInclusao = "system",
            UsuarioAlteracao = "system"
        };
        await _temaService.SalvarAsync(tema);
        return RedirectToAction("Index", "Home");
    }
}

