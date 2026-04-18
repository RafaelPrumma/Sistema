using Microsoft.EntityFrameworkCore;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using System.Globalization;

namespace Sistema.APP.Services;

public class MensagemAppService(IUnitOfWork uow, ILogAppService logService) : IMensagemAppService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ILogAppService _logService = logService;

    private static OperationResult<int> ValidarConteudo(string assunto, string corpo)
    {
        if (string.IsNullOrWhiteSpace(assunto))
            return new OperationResult<int>(false, "Assunto é obrigatório.");
        if (assunto.Length > 200)
            return new OperationResult<int>(false, "Assunto deve ter no máximo 200 caracteres.");
        if (string.IsNullOrWhiteSpace(corpo))
            return new OperationResult<int>(false, "Corpo da mensagem é obrigatório.");
        if (corpo.Length > 5000)
            return new OperationResult<int>(false, "Corpo da mensagem deve ter no máximo 5000 caracteres.");

        return new OperationResult<int>(true, string.Empty);
    }

    private static bool EhChefeSetor(Usuario usuario) =>
        usuario.Perfil?.Nome?.Contains("chefe", StringComparison.OrdinalIgnoreCase) == true;

    private static bool EhAdmin(Usuario usuario) =>
        usuario.Perfil?.Nome?.Contains("admin", StringComparison.OrdinalIgnoreCase) == true;

    private async Task<OperationResult<Usuario>> ObterAutorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _uow.Usuarios.Query().Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);
        if (usuario is null)
            return new OperationResult<Usuario>(false, "Usuário não encontrado.");

        return new OperationResult<Usuario>(true, string.Empty, usuario);
    }

    public async Task<PagedResult<Mensagem>> BuscarFeedAsync(int usuarioId, int page, int pageSize, FeedFiltroDto? filtro = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Mensagem> query = _uow.Mensagens.Query()
            .Where(m => m.Status == PublicacaoStatus.Ativa)
            .Where(m =>
                (m.Tipo == PublicacaoTipo.MensagemDireta && (m.DestinatarioId == usuarioId || m.RemetenteId == usuarioId)) ||
                (m.Tipo == PublicacaoTipo.PostSetor && (!m.PerfilId.HasValue || _uow.Usuarios.Query().Where(u => u.Id == usuarioId).Select(u => u.PerfilId).Contains(m.PerfilId.Value))) ||
                (m.Tipo == PublicacaoTipo.Aviso &&
                    (m.AvisoAudiencia == AvisoAudiencia.Todos
                     || (m.AvisoAudiencia == AvisoAudiencia.Setor && m.PerfilId.HasValue && _uow.Usuarios.Query().Where(u => u.Id == usuarioId).Select(u => u.PerfilId).Contains(m.PerfilId.Value))
                     || (m.AvisoAudiencia == AvisoAudiencia.Usuarios && (m.DestinatarioId == usuarioId || m.DestinatariosExplicitos.Any(d => d.UsuarioId == usuarioId)))
                     || (m.AvisoAudiencia == AvisoAudiencia.Grupo && m.AvisoGrupo != null))));

        if (filtro?.Tipo is not null)
            query = query.Where(m => m.Tipo == filtro.Tipo.Value);
        if (filtro?.PerfilId is not null)
            query = query.Where(m => m.PerfilId == filtro.PerfilId.Value);
        if (!string.IsNullOrWhiteSpace(filtro?.PalavraChave))
            query = query.Where(m => m.Assunto.Contains(filtro.PalavraChave) || m.Corpo.Contains(filtro.PalavraChave));
        if (filtro?.PrioridadeMinima is not null)
            query = query.Where(m => m.AvisoPrioridade == null || m.AvisoPrioridade >= filtro.PrioridadeMinima.Value);
        if (filtro?.SomenteNaoLidas == true)
            query = query.Where(m => !m.Leituras.Any(l => l.UsuarioId == usuarioId));

        query = query.Where(m => !m.AvisoValidoAte.HasValue || m.AvisoValidoAte >= DateTime.UtcNow)
                     .OrderByDescending(m => m.Fixada)
                     .ThenByDescending(m => m.DataInclusao);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Mensagem>(items, total, page, pageSize);
    }

    public async Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Mensagem> query = _uow.Mensagens.Query()
            .Where(m => m.Tipo == PublicacaoTipo.MensagemDireta)
            .Where(m => m.DestinatarioId == usuarioId);

        if (remetenteId.HasValue)
            query = query.Where(m => m.RemetenteId == remetenteId.Value);
        if (!string.IsNullOrWhiteSpace(palavraChave))
            query = query.Where(m => m.Assunto.Contains(palavraChave) || m.Corpo.Contains(palavraChave));
        if (inicio.HasValue)
            query = query.Where(m => m.DataInclusao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(m => m.DataInclusao <= fim.Value);

        query = query.OrderByDescending(m => m.DataInclusao);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Mensagem>(items, total, page, pageSize);
    }

    public async Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _uow.Mensagens.Query()
            .Where(m => m.Tipo == PublicacaoTipo.MensagemDireta)
            .Where(m => m.RemetenteId == usuarioId)
            .OrderByDescending(m => m.DataInclusao);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Mensagem>(items, total, page, pageSize);
    }

    public Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Mensagens.GetByIdAsync(id, cancellationToken);

    public async Task<Mensagem?> BuscarConversaAsync(int mensagemId, int usuarioId, CancellationToken cancellationToken = default)
    {
        var baseQuery = _uow.Mensagens.Query();

        var mensagem = await baseQuery.FirstOrDefaultAsync(m => m.Id == mensagemId, cancellationToken);
        if (mensagem is null)
            return null;

        var podeVer = await PodeVisualizarAsync(mensagem, usuarioId, cancellationToken);
        if (!podeVer)
            return null;

        var raiz = mensagem;
        while (raiz.MensagemPaiId.HasValue)
        {
            var pai = await baseQuery.FirstOrDefaultAsync(m => m.Id == raiz.MensagemPaiId.Value, cancellationToken);
            if (pai is null)
                break;
            raiz = pai;
        }

        var todas = new List<Mensagem> { raiz };
        var fronteira = new List<int> { raiz.Id };

        while (fronteira.Count != 0)
        {
            var filhos = await baseQuery
                .Where(m => m.MensagemPaiId.HasValue && fronteira.Contains(m.MensagemPaiId.Value))
                .OrderBy(m => m.DataInclusao)
                .ToListAsync(cancellationToken);

            if (filhos.Count == 0) break;

            var filhosVisiveis = new List<Mensagem>();
            foreach (var filho in filhos)
            {
                if (await PodeVisualizarAsync(filho, usuarioId, cancellationToken))
                {
                    filhosVisiveis.Add(filho);
                }
            }

            todas.AddRange(filhosVisiveis);
            fronteira = [.. filhos.Select(f => f.Id)];
        }

        var porPai = todas.Where(m => m.MensagemPaiId.HasValue).ToLookup(m => m.MensagemPaiId!.Value);
        foreach (var msg in todas)
        {
            msg.Respostas = [.. porPai[msg.Id].OrderBy(m => m.DataInclusao)];
        }

        return raiz;
    }

    private async Task<bool> PodeVisualizarAsync(Mensagem mensagem, int usuarioId, CancellationToken cancellationToken)
    {
        if (mensagem.Tipo == PublicacaoTipo.MensagemDireta)
            return mensagem.RemetenteId == usuarioId || mensagem.DestinatarioId == usuarioId || mensagem.DestinatariosExplicitos.Any(d => d.UsuarioId == usuarioId);

        if (mensagem.Tipo == PublicacaoTipo.PostSetor)
        {
            if (!mensagem.PerfilId.HasValue) return true;
            var perfilUsuario = await _uow.Usuarios.Query().Where(u => u.Id == usuarioId).Select(u => u.PerfilId).FirstOrDefaultAsync(cancellationToken);
            return perfilUsuario == mensagem.PerfilId;
        }

        if (mensagem.Tipo == PublicacaoTipo.Aviso)
        {
            return mensagem.AvisoAudiencia switch
            {
                AvisoAudiencia.Todos => true,
                AvisoAudiencia.Setor => (await _uow.Usuarios.Query().Where(u => u.Id == usuarioId).Select(u => u.PerfilId).FirstOrDefaultAsync(cancellationToken)) == mensagem.PerfilId,
                AvisoAudiencia.Usuarios => mensagem.DestinatarioId == usuarioId || mensagem.DestinatariosExplicitos.Any(d => d.UsuarioId == usuarioId),
                _ => true
            };
        }

        return false;
    }

    public async Task<OperationResult<int>> CriarPublicacaoAsync(int autorId, NovaMensagemDto dto, CancellationToken cancellationToken = default)
    {
        var autorResult = await ObterAutorAsync(autorId, cancellationToken);
        if (!autorResult.Success || autorResult.Data is null)
            return new OperationResult<int>(false, autorResult.Message);

        var autor = autorResult.Data;
        var validacao = ValidarConteudo(dto.Assunto, dto.Corpo);
        if (!validacao.Success)
            return validacao;

        if (dto.Tipo == PublicacaoTipo.MensagemDireta)
        {
            if (!dto.DestinatarioId.HasValue)
                return new OperationResult<int>(false, "Destinatário é obrigatório para mensagem direta.");

            return await EnviarAsync(autorId, dto.DestinatarioId.Value, dto.Assunto, dto.Corpo, dto.MensagemPaiId, cancellationToken);
        }

        if (dto.Tipo == PublicacaoTipo.PostSetor)
        {
            if (!EhChefeSetor(autor) && !EhAdmin(autor))
                return new OperationResult<int>(false, "Somente chefes de setor ou admin podem publicar posts de setor.");

            if (!dto.PerfilId.HasValue)
                return new OperationResult<int>(false, "Perfil/setor é obrigatório para post de setor.");

            if (!EhAdmin(autor) && autor.PerfilId != dto.PerfilId)
                return new OperationResult<int>(false, "Chefe de setor só pode publicar no próprio setor.");
        }

        if (dto.Tipo == PublicacaoTipo.Aviso)
        {
            if (dto.AvisoAudiencia == AvisoAudiencia.Todos && !EhAdmin(autor))
                return new OperationResult<int>(false, "Apenas admin/sistema pode publicar aviso global.");

            if (!EhAdmin(autor) && !EhChefeSetor(autor))
                return new OperationResult<int>(false, "Sem permissão para publicar aviso.");

            if (!EhAdmin(autor) && dto.AvisoAudiencia == AvisoAudiencia.Setor && dto.PerfilId != autor.PerfilId)
                return new OperationResult<int>(false, "Chefe de setor só pode publicar avisos para seu setor.");
        }

        var publicacao = new Mensagem
        {
            Tipo = dto.Tipo,
            Status = PublicacaoStatus.Ativa,
            AutorId = autorId,
            RemetenteId = autorId,
            DestinatarioId = dto.DestinatarioId,
            PerfilId = dto.PerfilId,
            Assunto = dto.Assunto.Trim(),
            Corpo = dto.Corpo.Trim(),
            MensagemPaiId = dto.MensagemPaiId,
            Lida = false,
            AvisoAudiencia = dto.AvisoAudiencia,
            AvisoPrioridade = dto.AvisoPrioridade,
            AvisoValidoAte = dto.AvisoValidoAte,
            Fixada = dto.Fixada
        };

        await _uow.Mensagens.AddAsync(publicacao, cancellationToken);

        if (dto.DestinatariosIds.Count != 0)
        {
            foreach (var usuarioId in dto.DestinatariosIds.Distinct())
            {
                publicacao.DestinatariosExplicitos.Add(new MensagemDestinatario
                {
                    UsuarioId = usuarioId,
                    Mensagem = publicacao
                });
            }
        }

        await _logService.RegistrarComunicacaoAsync(nameof(Mensagem), "CriarPublicacao", true, $"Publicação {dto.Tipo} criada", LogTipo.Sucesso, autorId.ToString(CultureInfo.InvariantCulture), $"PublicacaoId={publicacao.Id}", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<int>(true, string.Empty, publicacao.Id);
    }

    public async Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default)
    {
        var validacao = ValidarConteudo(assunto, corpo);
        if (!validacao.Success)
            return validacao;

        var assuntoLimpo = assunto.Trim();
        var corpoLimpo = corpo.Trim();

        var destinatario = await _uow.Usuarios.BuscarPorIdAsync(destinatarioId, cancellationToken);
        if (destinatario is null)
            return new OperationResult<int>(false, "Destinatário não encontrado");

        if (remetenteId.HasValue)
        {
            var remetente = await _uow.Usuarios.BuscarPorIdAsync(remetenteId.Value, cancellationToken);
            if (remetente is null)
                return new OperationResult<int>(false, "Remetente não encontrado");
        }

        var msg = new Mensagem
        {
            Tipo = PublicacaoTipo.MensagemDireta,
            Status = PublicacaoStatus.Ativa,
            AutorId = remetenteId,
            RemetenteId = remetenteId,
            DestinatarioId = destinatarioId,
            Assunto = assuntoLimpo,
            Corpo = corpoLimpo,
            MensagemPaiId = mensagemPaiId,
            Lida = false
        };
        await _uow.Mensagens.AddAsync(msg, cancellationToken);
        await _logService.RegistrarComunicacaoAsync(nameof(Mensagem), "EnviarDireta", true, "Mensagem direta enviada", LogTipo.Sucesso, (remetenteId ?? 0).ToString(CultureInfo.InvariantCulture), $"MensagemId={msg.Id};Destinatario={destinatarioId}", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<int>(true, string.Empty, msg.Id);
    }

    public async Task<OperationResult<List<int>>> EnviarParaPerfilAsync(int? remetenteId, int perfilId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default)
    {
        var validacao = ValidarConteudo(assunto, corpo);
        if (!validacao.Success)
            return new OperationResult<List<int>>(false, validacao.Message);

        var assuntoLimpo = assunto.Trim();
        var corpoLimpo = corpo.Trim();

        var usuarios = await _uow.Usuarios.BuscarPorPerfilAsync(perfilId, cancellationToken);
        if (usuarios.Count == 0)
            return new OperationResult<List<int>>(false, "Nenhum usuário ativo encontrado para o setor selecionado.");

        if (remetenteId.HasValue)
        {
            var remetente = await _uow.Usuarios.BuscarPorIdAsync(remetenteId.Value, cancellationToken);
            if (remetente is null)
                return new OperationResult<List<int>>(false, "Remetente não encontrado");
        }

        var mensagens = usuarios.Select(destinatario => new Mensagem
        {
            Tipo = PublicacaoTipo.MensagemDireta,
            Status = PublicacaoStatus.Ativa,
            AutorId = remetenteId,
            RemetenteId = remetenteId,
            DestinatarioId = destinatario.Id,
            PerfilId = perfilId,
            Assunto = assuntoLimpo,
            Corpo = corpoLimpo,
            MensagemPaiId = mensagemPaiId,
            Lida = false
        }).ToList();

        await _uow.Mensagens.AddRangeAsync(mensagens, cancellationToken);
        await _logService.RegistrarComunicacaoAsync(nameof(Mensagem), "EnviarParaPerfil", true, "Mensagens enviadas para setor", LogTipo.Sucesso, (remetenteId ?? 0).ToString(CultureInfo.InvariantCulture), $"PerfilId={perfilId};Quantidade={mensagens.Count}", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);

        return new OperationResult<List<int>>(true, string.Empty, mensagens.Select(m => m.Id).ToList());
    }

    public async Task<OperationResult> ReagirAsync(int publicacaoId, int usuarioId, TipoReacao tipoReacao, CancellationToken cancellationToken = default)
    {
        var publicacao = await _uow.Mensagens.GetByIdAsync(publicacaoId, cancellationToken);
        if (publicacao is null)
            return new OperationResult(false, "Publicação não encontrada.");

        if (!await PodeVisualizarAsync(publicacao, usuarioId, cancellationToken))
            return new OperationResult(false, "Usuário sem permissão para reagir a esta publicação.");

        var reacaoAtual = await _uow.Mensagens.QueryReacoes().FirstOrDefaultAsync(r => r.PublicacaoId == publicacaoId && r.UsuarioId == usuarioId, cancellationToken);
        if (reacaoAtual is null)
        {
            await _uow.Mensagens.AddReacaoAsync(new MensagemReacao
            {
                PublicacaoId = publicacaoId,
                UsuarioId = usuarioId,
                TipoReacao = tipoReacao,
                Data = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            reacaoAtual.TipoReacao = tipoReacao;
            reacaoAtual.Data = DateTime.UtcNow;
            _uow.Mensagens.UpdateReacao(reacaoAtual);
        }

        await _logService.RegistrarComunicacaoAsync(nameof(Mensagem), "Reagir", true, "Reação registrada", LogTipo.Sucesso, usuarioId.ToString(CultureInfo.InvariantCulture), $"PublicacaoId={publicacaoId};Reacao={tipoReacao}", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, string.Empty);
    }

    public async Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default)
    {
        var msg = await _uow.Mensagens.GetByIdAsync(id, cancellationToken);
        if (msg == null || !await PodeVisualizarAsync(msg, usuarioId, cancellationToken))
            return new OperationResult(false, "Mensagem não encontrada");

        var leituraJaExiste = await _uow.Mensagens.QueryLeituras().AnyAsync(l => l.PublicacaoId == id && l.UsuarioId == usuarioId, cancellationToken);
        if (!leituraJaExiste)
        {
            await _uow.Mensagens.AddLeituraAsync(new MensagemLeitura
            {
                PublicacaoId = id,
                UsuarioId = usuarioId,
                DataLeitura = DateTime.UtcNow,
                DataEntrega = msg.DataInclusao
            }, cancellationToken);
        }

        if (msg.DestinatarioId == usuarioId && !msg.Lida)
        {
            msg.Lida = true;
            msg.DataLeitura = DateTime.UtcNow;
            _uow.Mensagens.Update(msg);
        }

        await _logService.RegistrarComunicacaoAsync(nameof(Mensagem), "MarcarLida", true, "Publicação marcada como lida", LogTipo.Sucesso, usuarioId.ToString(CultureInfo.InvariantCulture), $"PublicacaoId={id}", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, string.Empty);
    }

    public async Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var total = await _uow.Mensagens.Query().CountAsync(m =>
            (m.DestinatarioId == usuarioId || m.DestinatariosExplicitos.Any(d => d.UsuarioId == usuarioId)) &&
            !m.Leituras.Any(l => l.UsuarioId == usuarioId), cancellationToken);
        return total;
    }
}
