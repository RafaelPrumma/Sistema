using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using AutoMapper;
using System;

namespace Sistema.MVC.Controllers
{
    public class MensagemController : Controller
    {
        private readonly IMensagemService _mensagemService;
        private readonly IMapper _mapper;

        public MensagemController(IMensagemService mensagemService, IMapper mapper)
        {
            _mensagemService = mensagemService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null) return RedirectToAction("Login", "Account");
            var result = await _mensagemService.BuscarCaixaEntradaAsync(userId.Value, page, pageSize, remetenteId, palavraChave, inicio, fim);
            var model = new MensagemViewModel
            {
                Mensagens = result.Items.Select(m => _mapper.Map<Sistema.APP.DTOs.MensagemDto>(m)).ToList(),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalCount,
                RemetenteId = remetenteId,
                PalavraChave = palavraChave,
                Inicio = inicio,
                Fim = fim
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Nova(int? mensagemPaiId = null, int? destinatarioId = null)
        {
            ViewBag.MensagemPaiId = mensagemPaiId;
            ViewBag.DestinatarioId = destinatarioId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Nova(int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null)
        {
            var remetenteId = HttpContext.Session.GetInt32("UserId");
            if (remetenteId is null) return RedirectToAction("Login", "Account");
            await _mensagemService.EnviarAsync(remetenteId, destinatarioId, assunto, corpo, mensagemPaiId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Detalhe(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null) return RedirectToAction("Login", "Account");
            var msg = await _mensagemService.BuscarPorIdAsync(id);
            if (msg == null) return NotFound();
            if (msg.DestinatarioId == userId) await _mensagemService.MarcarComoLidaAsync(id, userId.Value);
            return View(_mapper.Map<Sistema.APP.DTOs.MensagemDto>(msg));
        }
    }
}
