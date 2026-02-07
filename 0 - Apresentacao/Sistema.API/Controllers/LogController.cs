using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Entities;
using Sistema.APP.Services.Interfaces;
using System.Threading;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogController : ControllerBase
{
    private readonly ILogAppService _logs;

    public LogController(ILogAppService logs)
    {
        _logs = logs;
    }

    [HttpGet]
    public async Task<IEnumerable<Log>> Get([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] LogTipo? tipo, CancellationToken cancellationToken)
    {

        return await _logs.BuscarFiltradosAsync(inicio, fim, tipo, cancellationToken);
    }
}
