using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.MVC.Models;
using System.Security.Claims;
using System.Globalization;

namespace Sistema.MVC.Controllers
{
    public class MensagemController(IMensagemAppService mensagemService, IUsuarioAppService usuarioService, IPerfilAppService perfilService, IMapper mapper) : Controller
    {
        private const string PerfilPrefixo = "perfil:";
        private const string UsuarioPrefixo = "usuario:";

        private readonly IMensagemAppService _mensagemService = mensagemService;
        private readonly IUsuarioAppService _usuarioService = usuarioService;
        private readonly IPerfilAppService _perfilService = perfilService;
        private readonly IMapper _mapper = mapper;

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
                .OrderBy(u => gruposPerfis.TryGetValue(u.PerfilId, out SelectListGroup? value) ? value.Name : string.Empty)
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
                DestinatarioSelecionados = destinatariosSelecionados?.Distinct().ToList() ?? [],
                Destinatarios = opcoesDestinatarios,
                Perfis = perfis.Items.OrderBy(p => p.Nome).Select(p => new SelectListItem(p.Nome, p.Id.ToString(CultureInfo.InvariantCulture)))
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, PublicacaoTipo? tipo = null, int? perfilId = null, bool somenteNaoLidas = false, AvisoPrioridade? prioridadeMinima = null, string? palavraChave = null)
        {
            var userId = ObterUsuarioId();
            if (userId is null) return RedirectToAction("Login", "Account");

            var result = await _mensagemService.BuscarFeedAsync(userId.Value, page, pageSize, new FeedFiltroDto
            {
                Tipo = tipo,
                PerfilId = perfilId,
                SomenteNaoLidas = somenteNaoLidas,
                PrioridadeMinima = prioridadeMinima,
                PalavraChave = palavraChave
            });

            var model = new MensagemViewModel
            {
                Mensagens = [.. result.Items.Select(m =>
                {
                    var dto = _mapper.Map<MensagemDto>(m);
                    dto.PodeReagir = true;
                    dto.PodeResponder = true;
                    return dto;
                })],
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalCount,
                Tipo = tipo,
                PerfilId = perfilId,
                SomenteNaoLidas = somenteNaoLidas,
                PrioridadeMinima = prioridadeMinima,
                PalavraChave = palavraChave
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
                model.Perfis = viewModel.Perfis;
                return View(model);
            }

            if (model.Tipo == PublicacaoTipo.MensagemDireta)
            {
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

                if (errosEnvio.Count != 0)
                {
                    foreach (var erro in errosEnvio)
                    {
                        ModelState.AddModelError(string.Empty, erro);
                    }

                    var viewModel = await CriarNovaMensagemViewModelAsync(model.MensagemPaiId, model.DestinatarioSelecionados, model.Assunto);
                    model.Destinatarios = viewModel.Destinatarios;
                    model.Perfis = viewModel.Perfis;
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }

            var dto = new NovaMensagemDto
            {
                Tipo = model.Tipo,
                Assunto = model.Assunto,
                Corpo = model.Corpo,
                MensagemPaiId = model.MensagemPaiId,
                PerfilId = model.PerfilId,
                AvisoAudiencia = model.AvisoAudiencia,
                AvisoPrioridade = model.AvisoPrioridade,
                AvisoValidoAte = model.AvisoValidoAte,
                Fixada = model.Fixada
            };

            var resultado = await _mensagemService.CriarPublicacaoAsync(remetenteId.Value, dto);
            if (!resultado.Success)
            {
                ModelState.AddModelError(string.Empty, resultado.Message);
                var viewModel = await CriarNovaMensagemViewModelAsync(model.MensagemPaiId, model.DestinatarioSelecionados, model.Assunto);
                model.Destinatarios = viewModel.Destinatarios;
                model.Perfis = viewModel.Perfis;
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reagir(int id, TipoReacao tipoReacao)
        {
            var userId = ObterUsuarioId();
            if (userId is null) return RedirectToAction("Login", "Account");

            await _mensagemService.ReagirAsync(id, userId.Value, tipoReacao);
            return RedirectToAction(nameof(Detalhe), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Detalhe(int id)
        {
            var userId = ObterUsuarioId();
            if (userId is null) return RedirectToAction("Login", "Account");
            var cancellationToken = HttpContext.RequestAborted;
            var mensagemAtual = await _mensagemService.BuscarPorIdAsync(id, cancellationToken);
            if (mensagemAtual is null)
                return NotFound();

            await _mensagemService.MarcarComoLidaAsync(id, userId.Value, cancellationToken);

            var conversa = await _mensagemService.BuscarConversaAsync(id, userId.Value, cancellationToken);
            if (conversa == null) return NotFound();

            if (conversa.Respostas.Count != 0)
            {
                var naoLidas = EnumerarThread(conversa)
                    .Where(m => m.Id != id)
                    .Select(m => m.Id)
                    .ToList();

                foreach (var msgId in naoLidas)
                {
                    await _mensagemService.MarcarComoLidaAsync(msgId, userId.Value, cancellationToken);
                }
            }

            return View(_mapper.Map<MensagemThreadDto>(conversa));
        }

        private static IEnumerable<CORE.Entities.Mensagem> EnumerarThread(CORE.Entities.Mensagem raiz)
        {
            var pilha = new Stack<CORE.Entities.Mensagem>();
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
                Mensagens = [.. result.Items.Select(m => _mapper.Map<MensagemDto>(m))],
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalCount
            };
            return View(model);
        }
    }
}
