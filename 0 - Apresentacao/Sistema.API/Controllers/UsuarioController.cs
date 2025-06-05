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
    public async Task<IEnumerable<UsuarioDto>> Get()
    {
        var usuarios = await _service.GetAllAsync();
        return _mapper.Map<IEnumerable<UsuarioDto>>(usuarios);
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
}
