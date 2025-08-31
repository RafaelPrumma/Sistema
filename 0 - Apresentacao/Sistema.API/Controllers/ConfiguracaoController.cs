using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfiguracaoController : ControllerBase
{
	private readonly IConfiguracaoService _service;
	private readonly IMapper _mapper;

	public ConfiguracaoController(IConfiguracaoService service, IMapper mapper)
	{
		_service = service;
		_mapper = mapper;
	}

	[HttpGet("{agrupamento}")]
        public async Task<IEnumerable<ConfiguracaoDto>> Get(string agrupamento)
        {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _service.BuscarPorAgrupamentoAsync(agrupamento, cancellationToken);
                return _mapper.Map<IEnumerable<ConfiguracaoDto>>(result);
        }

	[HttpGet("{agrupamento}/{chave}")]
        public async Task<ActionResult<ConfiguracaoDto>> Get(string agrupamento, string chave)
        {
                var cancellationToken = HttpContext.RequestAborted;
                var entity = await _service.BuscarPorChaveAsync(agrupamento, chave, cancellationToken);
                if (entity is null) return NotFound();
                return _mapper.Map<ConfiguracaoDto>(entity);
        }

	[HttpPost]
        public async Task<ActionResult<ConfiguracaoDto>> Post(ConfiguracaoDto dto)
        {
                var cancellationToken = HttpContext.RequestAborted;
                var entity = _mapper.Map<Configuracao>(dto);
                var result = await _service.AdicionarAsync(entity, cancellationToken);
                var mapped = _mapper.Map<ConfiguracaoDto>(result);
                return CreatedAtAction(nameof(Get), new { agrupamento = mapped.Agrupamento, chave = mapped.Chave }, mapped);
        }

	[HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, ConfiguracaoDto dto)
        {
                if (id != dto.Id) return BadRequest();
                var cancellationToken = HttpContext.RequestAborted;
                var entity = _mapper.Map<Configuracao>(dto);
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