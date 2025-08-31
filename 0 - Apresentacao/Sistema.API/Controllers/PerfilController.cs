using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Sistema.APP.DTOs;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.CORE.Services.Interfaces;
namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerfilController : ControllerBase
{
    private readonly IPerfilService _service;
    private readonly IMapper _mapper;

    public PerfilController(IPerfilService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IEnumerable<PerfilDto>> Get(int page = 1, int pageSize = 10)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _service.BuscarTodosAsync(page, pageSize, cancellationToken);
        return _mapper.Map<IEnumerable<PerfilDto>>(result.Items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PerfilDto>> Get(int id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var perfil = await _service.BuscarPorIdAsync(id, cancellationToken);
        if (perfil is null) return NotFound();
        return _mapper.Map<PerfilDto>(perfil);
    }

    [HttpPost]
    public async Task<ActionResult<PerfilDto>> Post(PerfilDto dto)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var perfil = _mapper.Map<Perfil>(dto);
        var result = await _service.AdicionarAsync(perfil, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, _mapper.Map<PerfilDto>(result.Data));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, PerfilDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var cancellationToken = HttpContext.RequestAborted;
        var perfil = _mapper.Map<Perfil>(dto);
        var result = await _service.AtualizarAsync(perfil, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _service.RemoverAsync(id, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return NoContent();
    }
}
