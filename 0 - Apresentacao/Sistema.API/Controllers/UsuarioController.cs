using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.CORE.Interfaces;
using Sistema.CORE.Common;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly IMapper _mapper;

    public UsuarioController(IUsuarioService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IEnumerable<UsuarioDto>> Get(int page = 1, int pageSize = 10)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _service.BuscarTodosAsync(page, pageSize, cancellationToken);
        return _mapper.Map<IEnumerable<UsuarioDto>>(result.Items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> Get(int id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var usuario = await _service.BuscarPorIdAsync(id, cancellationToken);
        if (usuario is null) return NotFound();
        return _mapper.Map<UsuarioDto>(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Post(UsuarioDto dto)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var usuario = _mapper.Map<Usuario>(dto);
        var result = await _service.AdicionarAsync(usuario, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, _mapper.Map<UsuarioDto>(result.Data));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, UsuarioDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var cancellationToken = HttpContext.RequestAborted;
        var usuario = _mapper.Map<Usuario>(dto);
        var result = await _service.AtualizarAsync(usuario, cancellationToken);
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
