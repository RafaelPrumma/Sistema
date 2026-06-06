using Microsoft.AspNetCore.Mvc;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;

namespace Sistema.MVC.Controllers;

[AuthorizePermission("MinhasFinancas", Permissao.Visualizar)]
public class MinhasFinancasController(IMinhasFinancasAppService service) : Controller
{
    private readonly IMinhasFinancasAppService _service = service;

    [HttpGet("/MinhasFinancas")]
    [HttpGet("/MinhasFinancas/Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _service.ObterDashboardAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Documentos(string? termo, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        return View(await _service.BuscarDocumentosAsync(page, 25, termo, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Documento(int id, CancellationToken cancellationToken)
    {
        var (documento, conteudos) = await _service.ObterDocumentoAsync(id, cancellationToken);
        if (documento is null)
            return NotFound();

        ViewBag.Documento = documento;
        return View(conteudos);
    }

    [HttpGet]
    public async Task<IActionResult> OperacoesB3(string? termo, int? ano, string? classe, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        ViewBag.Ano = ano;
        ViewBag.Classe = classe;
        return View(await _service.BuscarOperacoesB3Async(page, 30, termo, ano, classe, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> OperacoesCripto(string? termo, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        return View(await _service.BuscarTransacoesCriptoAsync(page, 30, termo, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Posicoes(bool? abertas = null, CancellationToken cancellationToken = default)
    {
        ViewBag.Abertas = abertas;
        return View(await _service.BuscarPosicoesAsync(abertas, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Alertas(CancellationToken cancellationToken)
        => View(await _service.BuscarAlertasAsync(cancellationToken));
}
