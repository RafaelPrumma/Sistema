using Microsoft.AspNetCore.Mvc;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;

namespace Sistema.MVC.Controllers;

[AuthorizePermission("Log", Permissao.Visualizar)]
public class LogController(ILogAppService logService) : Controller
{
    private readonly ILogAppService _logService = logService;

    [HttpGet("/Logs")]
    [HttpGet("/Log")]
    public IActionResult Index() => View();

    [HttpGet("/Logs/Data")]
    public async Task<IActionResult> Data([FromQuery] DataTablesRequest request, string? modulo, string? tipo, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken)
    {
        LogModulo? moduloFiltro = Enum.TryParse<LogModulo>(modulo, true, out var m) ? m : null;
        LogTipo? tipoFiltro = Enum.TryParse<LogTipo>(tipo, true, out var t) ? t : null;
        var resultado = await _logService.BuscarDataTableAsync(request, inicio, fim, tipoFiltro, moduloFiltro, cancellationToken);
        return Json(resultado);
    }
}
