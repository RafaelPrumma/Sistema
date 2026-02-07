using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Sistema.APP.DTOs;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

using Sistema.APP.Services.Interfaces;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FuncionalidadeController : ControllerBase
{
    private readonly IFuncionalidadeAppService _service;
    private readonly IMapper _mapper;

    public FuncionalidadeController(IFuncionalidadeAppService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<PagedResult<FuncionalidadeDto>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _service.BuscarPaginadasAsync(page, pageSize, cancellationToken);
        var items = _mapper.Map<IEnumerable<FuncionalidadeDto>>(result.Items);
        return new PagedResult<FuncionalidadeDto>(items, result.TotalCount, result.Page, result.PageSize);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FuncionalidadeDto>> Get(int id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var obj = await _service.BuscarPorIdAsync(id, cancellationToken);
        if (obj is null) return NotFound();
        return _mapper.Map<FuncionalidadeDto>(obj);
    }

    [HttpPost]
    public async Task<ActionResult<FuncionalidadeDto>> Post(FuncionalidadeDto dto)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var entity = _mapper.Map<Funcionalidade>(dto);
        var result = await _service.AdicionarAsync(entity, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, _mapper.Map<FuncionalidadeDto>(result.Data));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, FuncionalidadeDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var cancellationToken = HttpContext.RequestAborted;
        var entity = _mapper.Map<Funcionalidade>(dto);
        await _service.AtualizarAsync(entity, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        await _service.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
