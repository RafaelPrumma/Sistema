using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;
using System.Security.Claims;

namespace Sistema.MVC.Controllers;

[AuthorizePermission("MinhasFinancas", Permissao.Visualizar)]
public class MinhasFinancasController(IMinhasFinancasAppService service) : Controller
{
    private readonly IMinhasFinancasAppService _service = service;

    [HttpGet("/MinhasFinancas")]
    [HttpGet("/MinhasFinancas/Index")]
    [HttpGet("/Financas")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _service.ObterDashboardAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportarPasta(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        try
        {
            // Roda em segundo plano (Hangfire) para não travar a requisição com PDFs grandes.
            // O usuarioId é capturado agora e usado para notificar quem disparou ao concluir.
            BackgroundJob.Enqueue<IMinhasFinancasAppService>(s => s.ImportarPastaMonitoradaAsync(usuarioId, CancellationToken.None));
            TempData["MensagemSucesso"] = "Importação iniciada em segundo plano. Você será notificado ao concluir — acompanhe na tela de Fila.";
        }
        catch
        {
            // Sem Hangfire configurado: importa de forma síncrona como fallback.
            await _service.ImportarPastaMonitoradaAsync(usuarioId, cancellationToken);
            TempData["MensagemSucesso"] = "Pasta financeira importada.";
        }

        return RedirectToAction(nameof(Index));
    }

    private int? ObterUsuarioId()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
            return userId.Value;

        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : null;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarCotacoes(CancellationToken cancellationToken)
    {
        await _service.AtualizarCotacoesAsync(cancellationToken);
        TempData["MensagemSucesso"] = "Atualização de cotações solicitada.";
        return RedirectToAction(nameof(Index));
    }

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

    [HttpGet]
    public async Task<IActionResult> Transacoes(string? termo, string? origem, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        ViewBag.Origem = origem;
        return View(await _service.BuscarTransacoesAsync(page, 30, termo, origem, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Resumo(DateTime? inicio, DateTime? fim, string? preset, CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        switch (preset)
        {
            case "mes": inicio = new DateTime(hoje.Year, hoje.Month, 1); fim = hoje; break;
            case "ano": inicio = new DateTime(hoje.Year, 1, 1); fim = hoje; break;
            case "12m": inicio = hoje.AddMonths(-12); fim = hoje; break;
            case "tudo": inicio = new DateTime(2000, 1, 1); fim = hoje; break;
        }

        ViewBag.Preset = preset;
        return View(await _service.ObterResumoAnaliticoAsync(inicio, fim, cancellationToken));
    }

    [HttpGet("/MinhasFinancas/ValidarAtivo")]
    public async Task<IActionResult> ValidarAtivo(string ticker, CancellationToken cancellationToken)
        => Json(await _service.ValidarAtivoAsync(ticker ?? string.Empty, cancellationToken));

    [HttpGet("/MinhasFinancas/Evolucao")]
    public async Task<IActionResult> Evolucao(CancellationToken cancellationToken)
        => Json(await _service.ObterEvolucaoPatrimonioAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transacoes(NovaTransacaoInput input, CancellationToken cancellationToken)
    {
        var resultado = await _service.RegistrarTransacaoManualAsync(input, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTransacao(int id, NovaTransacaoInput input, CancellationToken cancellationToken)
    {
        var resultado = await _service.EditarTransacaoAsync(id, input, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Transacoes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirTransacao(int id, CancellationToken cancellationToken)
    {
        var resultado = await _service.ExcluirTransacaoAsync(id, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Transacoes));
    }
}
