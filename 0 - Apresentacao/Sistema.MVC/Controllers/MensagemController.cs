using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema.CORE.Services.Interfaces;
using Sistema.MVC.Models;
using AutoMapper;
using Sistema.CORE.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sistema.MVC.Controllers
{
    public class MensagemController : Controller
    {
        private const string PerfilPrefixo = "perfil:";
        private const string UsuarioPrefixo = "usuario:";

        private readonly IMensagemService _mensagemService;
        private readonly IUsuarioService _usuarioService;
        private readonly IPerfilService _perfilService;
        private readonly IMapper _mapper;

        public MensagemController(IMensagemService mensagemService, IUsuarioService usuarioService, IPerfilService perfilService, IMapper mapper)
        {
            _mensagemService = mensagemService;
            _usuarioService = usuarioService;
            _perfilService = perfilService;
            _mapper = mapper;
        }

        private int? ObterUsuarioId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
                return userId.Value;

            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claimId, out var parsedId))
            {
                HttpContext.Session.SetInt32("UserId", parsedId);
                return parsedId;
            }

            return null;
        }

        private async Task<NovaMensagemViewModel> CriarNovaMensagemViewModelAsync(int? mensagemPaiId, IEnumerable<string>? destinatariosSelecionados = null, string? assunto = null)
        {
            var usuarios = await _usuarioService.BuscarTodosAsync(1, 1000);
            var perfis = await _perfilService.BuscarTodosAsync(1, 1000);
            var remetenteId = ObterUsuarioId();

            var gruposPerfis = perfis.Items
                .OrderBy(p => p.Nome)
                .ToDictionary(p => p.Id, p => new SelectListGroup { Name = p.Nome });

            var opcoesDestinatarios = new List<SelectListItem>();

            foreach (var perfil in perfis.Items.OrderBy(p => p.Nome))
            {
                opcoesDestinatarios.Add(new SelectListItem
                {
                    Text = $"Todos os {perfil.Nome}",
                    Value = $"{PerfilPrefixo}{perfil.Id}",
                    Group = gruposPerfis[perfil.Id]
                });
            }

            var destinatarios = usuarios.Items
                .Where(u => !remetenteId.HasValue || u.Id != remetenteId.Value)
                .OrderBy(u => gruposPerfis.ContainsKey(u.PerfilId) ? gruposPerfis[u.PerfilId].Name : string.Empty)
                .ThenBy(u => u.Nome)
                .Select(u => new SelectListItem
                {
                    Text = $"{u.Nome} (#{u.Id})",
                    Value = $"{UsuarioPrefixo}{u.Id}",
                    Group = gruposPerfis.TryGetValue(u.PerfilId, out var group) ? group : null
                })
                .ToList();

            opcoesDestinatarios.AddRange(destinatarios);

            return new NovaMensagemViewModel
            {
                MensagemPaiId = mensagemPaiId,
                Assunto = assunto ?? string.Empty,
                DestinatarioSelecionados = destinatariosSelecionados?.Distinct().ToList() ?? new List<string>(),
                Destinatarios = opcoesDestinatarios
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null)
        {
            var userId = ObterUsuarioId();
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
        public async Task<IActionResult> Nova(int? mensagemPaiId = null, int? destinatarioId = null)
        {
            var remetenteId = ObterUsuarioId();
            if (remetenteId is null) return RedirectToAction("Login", "Account");
            var destinatariosPreSelecionados = new List<string>();
            string? assunto = null;

            if (mensagemPaiId.HasValue)
            {
                var mensagemPai = await _mensagemService.BuscarPorIdAsync(mensagemPaiId.Value, HttpContext.RequestAborted);
                if (mensagemPai != null)
                {
                    assunto = mensagemPai.Assunto;
                    if (mensagemPai.RemetenteId.HasValue)
                    {
                        destinatariosPreSelecionados.Add($"{UsuarioPrefixo}{mensagemPai.RemetenteId.Value}");
                    }
                }
            }

            if (destinatarioId.HasValue)
            {
                destinatariosPreSelecionados.Add($"{UsuarioPrefixo}{destinatarioId.Value}");
            }

            var model = await CriarNovaMensagemViewModelAsync(mensagemPaiId, destinatariosPreSelecionados, assunto);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nova(NovaMensagemViewModel model)
        {
            var remetenteId = ObterUsuarioId();
            if (remetenteId is null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var viewModel = await CriarNovaMensagemViewModelAsync(model.MensagemPaiId, model.DestinatarioSelecionados, model.Assunto);
                model.Destinatarios = viewModel.Destinatarios;
                return View(model);
            }

            var errosEnvio = new List<string>();

            foreach (var destinatario in model.DestinatarioSelecionados.Distinct())
            {
                if (destinatario.StartsWith(PerfilPrefixo, StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(destinatario.Replace(PerfilPrefixo, string.Empty), out var perfilId))
                    {
                        errosEnvio.Add($"Destino inválido: {destinatario}.");
                        continue;
                    }

                    var resultadoPerfil = await _mensagemService.EnviarParaPerfilAsync(remetenteId.Value, perfilId, model.Assunto, model.Corpo, model.MensagemPaiId);
                    if (!resultadoPerfil.Success)
                    {
                        errosEnvio.Add(resultadoPerfil.Message);
                    }
                }
                else if (destinatario.StartsWith(UsuarioPrefixo, StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(destinatario.Replace(UsuarioPrefixo, string.Empty), out var destinatarioId))
                    {
                        errosEnvio.Add($"Destino inválido: {destinatario}.");
                        continue;
                    }

                    var resultadoDestinatario = await _mensagemService.EnviarAsync(remetenteId.Value, destinatarioId, model.Assunto, model.Corpo, model.MensagemPaiId);
                    if (!resultadoDestinatario.Success)
                    {
                        errosEnvio.Add(resultadoDestinatario.Message);
                    }
                }
                else
                {
                    errosEnvio.Add($"Destino não reconhecido: {destinatario}.");
                }
            }

            if (errosEnvio.Any())
            {
                foreach (var erro in errosEnvio)
                {
                    ModelState.AddModelError(string.Empty, erro);
                }

                var viewModel = await CriarNovaMensagemViewModelAsync(model.MensagemPaiId, model.DestinatarioSelecionados, model.Assunto);
                model.Destinatarios = viewModel.Destinatarios;
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Detalhe(int id)
        {
            var userId = ObterUsuarioId();
            if (userId is null) return RedirectToAction("Login", "Account");
            var cancellationToken = HttpContext.RequestAborted;
            var mensagemAtual = await _mensagemService.BuscarPorIdAsync(id, cancellationToken);
            if (mensagemAtual is null || (mensagemAtual.RemetenteId != userId.Value && mensagemAtual.DestinatarioId != userId.Value))
                return NotFound();

            if (mensagemAtual.DestinatarioId == userId.Value)
                await _mensagemService.MarcarComoLidaAsync(id, userId.Value, cancellationToken);

            var conversa = await _mensagemService.BuscarConversaAsync(id, userId.Value, cancellationToken);
            if (conversa == null) return NotFound();

            if (conversa.Respostas.Any())
            {
                var naoLidas = EnumerarThread(conversa)
                    .Where(m => m.Id != id)
                    .Where(m => m.DestinatarioId == userId.Value && !m.Lida)
                    .Select(m => m.Id)
                    .ToList();

                foreach (var msgId in naoLidas)
                {
                    await _mensagemService.MarcarComoLidaAsync(msgId, userId.Value, cancellationToken);
                }
            }

            return View(_mapper.Map<Sistema.APP.DTOs.MensagemThreadDto>(conversa));
        }

        private static IEnumerable<Sistema.CORE.Entities.Mensagem> EnumerarThread(Sistema.CORE.Entities.Mensagem raiz)
        {
            var pilha = new Stack<Sistema.CORE.Entities.Mensagem>();
            pilha.Push(raiz);
            while (pilha.Count > 0)
            {
                var atual = pilha.Pop();
                yield return atual;
                foreach (var filha in atual.Respostas)
                {
                    pilha.Push(filha);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CaixaSaida(int page = 1, int pageSize = 20)
        {
            var userId = ObterUsuarioId();
            if (userId is null) return RedirectToAction("Login", "Account");
            var result = await _mensagemService.BuscarCaixaSaidaAsync(userId.Value, page, pageSize);
            var model = new MensagemViewModel
            {
                Mensagens = result.Items.Select(m => _mapper.Map<Sistema.APP.DTOs.MensagemDto>(m)).ToList(),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalCount
            };
            return View(model);
        }
    }
}
