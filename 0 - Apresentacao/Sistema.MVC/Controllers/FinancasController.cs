using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;
using System.Security.Claims;

namespace Sistema.MVC.Controllers;

[AuthorizePermission("Financas", Permissao.Visualizar)]
public class FinancasController(IFinancasAppService service) : Controller
{
    private readonly IFinancasAppService _service = service;

    [HttpGet("/Financas")]
    [HttpGet("/Financas/Index")]
    public IActionResult Index()
        => View();

    [HttpGet("/Financas/Dashboard/Preparar")]
    public async Task<IActionResult> PrepararDashboard(CancellationToken cancellationToken)
    {
        await _service.PrepararDashboardAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("/Financas/Dashboard/Patrimonio")]
    public async Task<IActionResult> DashboardPatrimonio(CancellationToken cancellationToken)
        => Json(await _service.ObterPatrimonioDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Carteiras")]
    public async Task<IActionResult> DashboardCarteiras(CancellationToken cancellationToken)
        => PartialView("_DashboardCarteiras", await _service.ObterCarteirasDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Importacao")]
    public async Task<IActionResult> DashboardImportacao(CancellationToken cancellationToken)
        => PartialView("_DashboardImportacao", await _service.ObterImportacaoDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Posicoes")]
    public async Task<IActionResult> DashboardPosicoes(CancellationToken cancellationToken)
        => PartialView("_DashboardPosicoes", await _service.ObterPosicoesDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Alertas")]
    public async Task<IActionResult> DashboardAlertas(CancellationToken cancellationToken)
        => PartialView("_DashboardAlertas", await _service.ObterAlertasDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Proventos")]
    public async Task<IActionResult> DashboardProventos(CancellationToken cancellationToken)
        => PartialView("_DashboardProventos", await _service.ObterProventosDashboardAsync(cancellationToken));

    [HttpGet("/Financas/Dashboard/Reconciliacao")]
    public async Task<IActionResult> DashboardReconciliacao(CancellationToken cancellationToken)
        => PartialView("_DashboardReconciliacao", await _service.ObterReconciliacaoDashboardAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportarPasta(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        try
        {
            // Roda em segundo plano (Hangfire) para não travar a requisição com PDFs grandes.
            // O usuarioId é capturado agora e usado para notificar quem disparou ao concluir.
            BackgroundJob.Enqueue<IFinancasAppService>(s => s.ImportarPastaMonitoradaAsync(usuarioId, CancellationToken.None));
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
    public async Task<IActionResult> Proventos(string? termo, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        return View(await _service.BuscarProventosAsync(page, 25, termo, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarProventos(CancellationToken cancellationToken)
    {
        try
        {
            // Busca proventos (Brapi p/ B3 + earn da Binance) em segundo plano — pode levar alguns segundos.
            BackgroundJob.Enqueue<IFinancasAppService>(s => s.AtualizarProventosAsync(CancellationToken.None));
            TempData["MensagemSucesso"] = "Busca de proventos iniciada em segundo plano. Atualize a página em instantes.";
        }
        catch
        {
            await _service.AtualizarProventosAsync(cancellationToken);
            TempData["MensagemSucesso"] = "Proventos atualizados.";
        }

        return RedirectToAction(nameof(Proventos));
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
    public IActionResult Transacoes(string? origem)
    {
        ViewBag.Origem = origem;
        return View();
    }

    [HttpGet("/Financas/TransacoesData")]
    public async Task<IActionResult> TransacoesData([FromQuery] DataTablesRequest request, string? origem, CancellationToken cancellationToken)
        => Json(await _service.BuscarTransacoesDataTableAsync(request, origem, cancellationToken));

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

    [HttpGet("/Financas/ValidarAtivo")]
    public async Task<IActionResult> ValidarAtivo(string ticker, CancellationToken cancellationToken)
        => Json(await _service.ValidarAtivoAsync(ticker ?? string.Empty, cancellationToken));

    [HttpGet("/Financas/Evolucao")]
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

    [HttpGet]
    public async Task<IActionResult> Eventos(string? termo, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewBag.Termo = termo;
        return View(await _service.BuscarEventosCorporativosAsync(page, 25, termo, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eventos(NovoEventoCorporativoInput input, CancellationToken cancellationToken)
    {
        var resultado = await _service.RegistrarEventoCorporativoManualAsync(input, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Eventos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEvento(int id, NovoEventoCorporativoInput input, CancellationToken cancellationToken)
    {
        var resultado = await _service.EditarEventoCorporativoAsync(id, input, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Eventos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEvento(int id, CancellationToken cancellationToken)
    {
        var resultado = await _service.ExcluirEventoCorporativoAsync(id, cancellationToken);
        if (resultado.Sucesso)
            TempData["MensagemSucesso"] = resultado.Mensagem;
        else
            TempData["MensagemErro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Eventos));
    }

    [HttpGet("/Financas/IR")]
    public async Task<IActionResult> IR(int? ano, CancellationToken cancellationToken)
    {
        var anoAlvo = ano ?? DateTime.UtcNow.Year - 1; // declaração cobre o ano-calendário anterior.
        ViewBag.Ano = anoAlvo;
        return View(await _service.ObterApuracaoIrAsync(anoAlvo, cancellationToken));
    }

    [HttpGet("/Financas/IR/Exportar")]
    public async Task<IActionResult> ExportarIR(int ano, CancellationToken cancellationToken)
    {
        var bytes = await _service.ExportarApuracaoIrExcelAsync(ano, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"IR-{ano}.xlsx");
    }
}
