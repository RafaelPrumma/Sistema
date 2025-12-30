using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.CORE.Services.Interfaces;
using System;
using System.Security.Claims;

namespace Sistema.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MensagemController : ControllerBase
    {
        private readonly IMensagemService _mensagemService;
        private readonly IMapper _mapper;

        public MensagemController(IMensagemService mensagemService, IMapper mapper)
        {
            _mensagemService = mensagemService;
            _mapper = mapper;
        }

        private int? ObterUsuarioIdAutenticado()
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimId, out var usuarioId) ? usuarioId : null;
        }

        [HttpGet("entrada")]
        public async Task<IActionResult> Entrada(int page = 1, int pageSize = 20, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.BuscarCaixaEntradaAsync(usuarioId.Value, page, pageSize, remetenteId, palavraChave, inicio, fim, cancellationToken);
            var dto = result.Items.Select(m => _mapper.Map<MensagemDto>(m));
            return Ok(new { result.TotalCount, result.Page, result.PageSize, Items = dto });
        }

        [HttpGet("saida")]
        public async Task<IActionResult> Saida(int page = 1, int pageSize = 20)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.BuscarCaixaSaidaAsync(usuarioId.Value, page, pageSize, cancellationToken);
            var dto = result.Items.Select(m => _mapper.Map<MensagemDto>(m));
            return Ok(new { result.TotalCount, result.Page, result.PageSize, Items = dto });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            var msg = await _mensagemService.BuscarPorIdAsync(id, cancellationToken);
            if (msg == null || (msg.RemetenteId != usuarioId.Value && msg.DestinatarioId != usuarioId.Value))
                return NotFound();
            return Ok(_mapper.Map<MensagemDto>(msg));
        }

        [HttpGet("{id}/conversa")]
        public async Task<IActionResult> Conversa(int id)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            var conversa = await _mensagemService.BuscarConversaAsync(id, usuarioId.Value, cancellationToken);
            if (conversa is null) return NotFound();
            return Ok(_mapper.Map<MensagemThreadDto>(conversa));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NovaMensagemDto dto)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            if (dto.PerfilId.HasValue)
            {
                var resultadoGrupo = await _mensagemService.EnviarParaPerfilAsync(usuarioId, dto.PerfilId.Value, dto.Assunto, dto.Corpo, dto.MensagemPaiId, cancellationToken);
                if (!resultadoGrupo.Success) return BadRequest(resultadoGrupo.Message);
                return Created(string.Empty, resultadoGrupo.Data);
            }

            var result = await _mensagemService.EnviarAsync(usuarioId, dto.DestinatarioId, dto.Assunto, dto.Corpo, dto.MensagemPaiId, cancellationToken);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(Get), new { id = result.Data }, result.Data);
        }

        [HttpPost("{id}/ler")]
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            var usuarioId = ObterUsuarioIdAutenticado();
            if (usuarioId is null) return Unauthorized();
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.MarcarComoLidaAsync(id, usuarioId.Value, cancellationToken);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
    }
}
