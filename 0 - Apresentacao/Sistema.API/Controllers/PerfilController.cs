using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.CORE.Common;
using Microsoft.AspNetCore.Authorization;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    public async Task<IEnumerable<PerfilDto>> Get()
    {
        var perfis = await _service.GetAllAsync();
        return _mapper.Map<IEnumerable<PerfilDto>>(perfis);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PerfilDto>> Get(int id)
    {
        var perfil = await _service.GetByIdAsync(id);
        if (perfil is null) return NotFound();
        return _mapper.Map<PerfilDto>(perfil);
    }

    [HttpPost]
    public async Task<ActionResult<PerfilDto>> Post(PerfilDto dto)
    {
        var perfil = _mapper.Map<Perfil>(dto);
        var result = await _service.AddAsync(perfil);
        if (!result.Success) return BadRequest(result.Message);
        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, _mapper.Map<PerfilDto>(result.Data));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, PerfilDto dto)
    {
        if (id != dto.Id) return BadRequest();
        var perfil = _mapper.Map<Perfil>(dto);
        var result = await _service.UpdateAsync(perfil);
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
