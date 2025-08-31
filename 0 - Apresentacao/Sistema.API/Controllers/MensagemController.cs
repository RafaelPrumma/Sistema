using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Sistema.APP.DTOs;
using Sistema.CORE.Services.Interfaces;
using System;

namespace Sistema.API.Controllers
{
    [ApiController]
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

        [HttpGet("entrada")]
        public async Task<IActionResult> Entrada(int usuarioId, int page = 1, int pageSize = 20, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.BuscarCaixaEntradaAsync(usuarioId, page, pageSize, remetenteId, palavraChave, inicio, fim, cancellationToken);
            var dto = result.Items.Select(m => _mapper.Map<MensagemDto>(m));
            return Ok(new { result.TotalItems, result.Page, result.PageSize, Items = dto });
        }

        [HttpGet("saida")]
        public async Task<IActionResult> Saida(int usuarioId, int page = 1, int pageSize = 20)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.BuscarCaixaSaidaAsync(usuarioId, page, pageSize, cancellationToken);
            var dto = result.Items.Select(m => _mapper.Map<MensagemDto>(m));
            return Ok(new { result.TotalItems, result.Page, result.PageSize, Items = dto });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var msg = await _mensagemService.BuscarPorIdAsync(id, cancellationToken);
            if (msg == null) return NotFound();
            return Ok(_mapper.Map<MensagemDto>(msg));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] NovaMensagemDto dto)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.EnviarAsync(dto.RemetenteId, dto.DestinatarioId, dto.Assunto, dto.Corpo, dto.MensagemPaiId, cancellationToken);
            if (!result.Success) return BadRequest(result.Message);
            return CreatedAtAction(nameof(Get), new { id = result.Data }, result.Data);
        }

        [HttpPost("{id}/ler")]
        public async Task<IActionResult> MarcarComoLida(int id, int usuarioId)
        {
            var cancellationToken = HttpContext.RequestAborted;
            var result = await _mensagemService.MarcarComoLidaAsync(id, usuarioId, cancellationToken);
            if (!result.Success) return BadRequest(result.Message);
            return NoContent();
        }
    }
}
