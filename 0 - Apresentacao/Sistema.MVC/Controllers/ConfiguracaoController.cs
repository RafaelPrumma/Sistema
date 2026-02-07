using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.APP.Services.Interfaces;
using Sistema.MVC.Models;

namespace Sistema.MVC.Controllers;

public class ConfiguracaoController(IConfiguracaoAppService service) : Controller
{
    private readonly IConfiguracaoAppService _service = service;

	[HttpGet]
    public async Task<IActionResult> Index(string agrupamento = "AzureAd")
    {
        var configs = await _service.BuscarPorAgrupamentoAsync(agrupamento);
        var model = new ConfiguracaoIndexViewModel
        {
            Agrupamento = agrupamento,
            Configuracoes = [.. configs.Select(c => new ConfiguracaoViewModel
            {
                Id = c.Id,
                Agrupamento = c.Agrupamento,
                Chave = c.Chave,
                Valor = c.Valor,
                Tipo = c.Tipo,
                Descricao = c.Descricao,
                Ativo = c.Ativo
            })]
		};
        return View(model);
    }

    [HttpGet]
    public IActionResult Create(string agrupamento)
    {
        var model = new ConfiguracaoViewModel { Agrupamento = agrupamento };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ConfiguracaoViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var entity = new Configuracao
        {
            Agrupamento = model.Agrupamento,
            Chave = model.Chave,
            Valor = model.Valor,
            Tipo = model.Tipo,
            Descricao = model.Descricao,
            Ativo = model.Ativo,
            UsuarioInclusao = "system"
        };
        await _service.AdicionarAsync(entity);
        return RedirectToAction(nameof(Index), new { agrupamento = model.Agrupamento });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string agrupamento, string chave)
    {
        var entity = await _service.BuscarPorChaveAsync(agrupamento, chave);
        if (entity is null) return NotFound();
        var model = new ConfiguracaoViewModel
        {
            Id = entity.Id,
            Agrupamento = entity.Agrupamento,
            Chave = entity.Chave,
            Valor = entity.Valor,
            Tipo = entity.Tipo,
            Descricao = entity.Descricao,
            Ativo = entity.Ativo
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ConfiguracaoViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var entity = new Configuracao
        {
            Id = model.Id,
            Agrupamento = model.Agrupamento,
            Chave = model.Chave,
            Valor = model.Valor,
            Tipo = model.Tipo,
            Descricao = model.Descricao,
            Ativo = model.Ativo,
            UsuarioAlteracao = "system"
        };
        await _service.AtualizarAsync(entity);
        return RedirectToAction(nameof(Index), new { agrupamento = model.Agrupamento });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, string agrupamento)
    {
        await _service.RemoverAsync(id);
        return RedirectToAction(nameof(Index), new { agrupamento });
    }
}
