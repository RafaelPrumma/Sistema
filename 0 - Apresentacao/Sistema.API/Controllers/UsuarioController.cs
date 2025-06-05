using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.CORE.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<Usuario> _hasher;

    public UsuarioController(IUsuarioService service, IMapper mapper, IPasswordHasher<Usuario> hasher)
    {
        _service = service;
        _mapper = mapper;
        _hasher = hasher;
    }

    [HttpGet]
    public async Task<PagedResult<UsuarioDto>> Get([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim,
        [FromQuery] int? perfilId, [FromQuery] bool? ativo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var usuarios = inicio.HasValue || fim.HasValue || perfilId.HasValue || ativo.HasValue
            ? await _service.GetFilteredAsync(inicio, fim, perfilId, ativo, page, pageSize)
            : await _service.GetAllAsync(page, pageSize);
        var items = _mapper.Map<IEnumerable<UsuarioDto>>(usuarios.Items);
        return new PagedResult<UsuarioDto>(items, usuarios.TotalCount, usuarios.Page, usuarios.PageSize);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> Get(int id)
    {
        var usuario = await _service.GetByIdAsync(id);
        if (usuario is null) return NotFound();
        return _mapper.Map<UsuarioDto>(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Post(UsuarioDto dto)
    {
        var usuario = _mapper.Map<Usuario>(dto);
        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, dto.Senha);
        }
        var result = await _service.AddAsync(usuario);
        if (!result.Success) return BadRequest(result.Message);
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, _mapper.Map<UsuarioDto>(result.Data));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, UsuarioDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var usuario = _mapper.Map<Usuario>(dto);
        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, dto.Senha);
        }
        var result = await _service.UpdateAsync(usuario);
        if (!result.Success) return BadRequest(result.Message);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return NoContent();
    }

    [HttpPatch("{id}/ativo")]
    public async Task<IActionResult> AlterarAtivo(int id, [FromQuery] bool ativo, [FromQuery] string usuario)
    {
        var result = await _service.AlterarAtivoAsync(id, ativo, usuario);
        if (!result.Success) return BadRequest(result.Message);
        return NoContent();
    }
}
