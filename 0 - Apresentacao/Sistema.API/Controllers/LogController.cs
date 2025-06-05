using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Interfaces;
using Sistema.CORE.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogController : ControllerBase
{
    private readonly ILogRepository _logs;

    public LogController(ILogRepository logs)
    {
        _logs = logs;
    }

    [HttpGet]
    public async Task<IEnumerable<Log>> Get([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] LogTipo? tipo)
    {
        return await _logs.GetFilteredAsync(inicio, fim, tipo);
    }
}
