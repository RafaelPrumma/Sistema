using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services
{
    /// <summary>
    /// Serviço de mensagens responsável por consultas, envio e atualização de status de leitura.
    /// </summary>
    public class MensagemService(IUnitOfWork uow) : IMensagemService
    {
        private readonly IUnitOfWork _uow = uow;

        /// <summary>
        /// Valida assunto e corpo de uma mensagem garantindo preenchimento e limites de tamanho aceitáveis.
        /// </summary>
        /// <param name="assunto">Texto informado como assunto.</param>
        /// <param name="corpo">Corpo detalhado da mensagem.</param>
        /// <returns>Resultado indicando sucesso ou falha com a justificativa.</returns>
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

        /// <summary>
        /// Busca mensagens recebidas pelo usuário aplicando filtros opcionais e paginação.
        /// </summary>
        /// <param name="usuarioId">Identificador do destinatário.</param>
        /// <param name="page">Página solicitada (base 1).</param>
        /// <param name="pageSize">Quantidade de itens por página.</param>
        /// <param name="remetenteId">Filtro opcional pelo remetente.</param>
        /// <param name="palavraChave">Termo opcional pesquisado em assunto ou corpo.</param>
        /// <param name="inicio">Data inicial do período desejado.</param>
        /// <param name="fim">Data final do período desejado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado paginado com as mensagens encontradas.</returns>
        public async Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default)
        {
            IQueryable<Mensagem> query = _uow.Mensagens.Query()
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
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

        /// <summary>
        /// Busca mensagens enviadas por um usuário aplicando paginação padrão.
        /// </summary>
        /// <param name="usuarioId">Identificador do remetente.</param>
        /// <param name="page">Página solicitada (base 1).</param>
        /// <param name="pageSize">Quantidade máxima de itens por página.</param>
        /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
        /// <returns>Lista paginada das mensagens enviadas.</returns>
        public async Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _uow.Mensagens.Query()
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario)
                .Where(m => m.RemetenteId == usuarioId)
                .OrderByDescending(m => m.DataInclusao);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResult<Mensagem>(items, total, page, pageSize);
        }

        /// <summary>
        /// Obtém uma mensagem específica incluindo dados de remetente, destinatário e mensagem pai.
        /// </summary>
        /// <param name="id">Identificador único da mensagem.</param>
        /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
        /// <returns>Instância encontrada ou nula quando inexistente.</returns>
        public Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Mensagens.GetByIdAsync(id, cancellationToken);

        /// <summary>
        /// Recupera toda a conversa relacionada a uma mensagem desde a origem, validando a participação do usuário.
        /// </summary>
        /// <param name="mensagemId">Mensagem base da consulta.</param>
        /// <param name="usuarioId">Usuário que solicita a conversa (precisa ser participante).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>A mensagem raiz com suas respostas encadeadas ou nulo caso o usuário não tenha acesso.</returns>
        public async Task<Mensagem?> BuscarConversaAsync(int mensagemId, int usuarioId, CancellationToken cancellationToken = default)
        {
            var baseQuery = _uow.Mensagens.Query()
                .Include(m => m.Remetente)
                .Include(m => m.Destinatario);

            var mensagem = await baseQuery
                .FirstOrDefaultAsync(m => m.Id == mensagemId, cancellationToken);
            if (mensagem is null || (mensagem.RemetenteId != usuarioId && mensagem.DestinatarioId != usuarioId))
                return null;

            var raiz = mensagem;
            while (raiz.MensagemPaiId.HasValue)
            {
                var pai = await baseQuery
                    .FirstOrDefaultAsync(m => m.Id == raiz.MensagemPaiId.Value, cancellationToken);
                if (pai is null)
                    break;
                if (pai.RemetenteId != usuarioId && pai.DestinatarioId != usuarioId)
                    return null;
                raiz = pai;
            }

            var todas = new List<Mensagem> { raiz };
            var fronteira = new List<int> { raiz.Id };

            while (fronteira.Count != 0)
            {
                var filhos = await baseQuery
                    .Where(m => m.MensagemPaiId.HasValue && fronteira.Contains(m.MensagemPaiId.Value))
                    .Where(m => m.RemetenteId == usuarioId || m.DestinatarioId == usuarioId)
                    .OrderBy(m => m.DataInclusao)
                    .ToListAsync(cancellationToken);

                if (filhos.Count == 0) break;

                todas.AddRange(filhos);
                fronteira = [.. filhos.Select(f => f.Id)];
            }

            var porPai = todas.Where(m => m.MensagemPaiId.HasValue).ToLookup(m => m.MensagemPaiId!.Value);
            foreach (var msg in todas)
            {
                msg.Respostas = [.. porPai[msg.Id].OrderBy(m => m.DataInclusao)];
            }

            return raiz;
        }

        /// <summary>
        /// Envia uma nova mensagem para um destinatário validando conteúdo e existência dos usuários envolvidos.
        /// </summary>
        /// <param name="remetenteId">Identificador opcional do remetente.</param>
        /// <param name="destinatarioId">Identificador do destinatário obrigatório.</param>
        /// <param name="assunto">Assunto a ser enviado.</param>
        /// <param name="corpo">Corpo do texto da mensagem.</param>
        /// <param name="mensagemPaiId">Identificador opcional da mensagem respondida.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado contendo sucesso e o id da nova mensagem.</returns>
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
                RemetenteId = remetenteId,
                DestinatarioId = destinatarioId,
                Assunto = assuntoLimpo,
                Corpo = corpoLimpo,
                MensagemPaiId = mensagemPaiId,
                Lida = false
            };
            await _uow.Mensagens.AddAsync(msg, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult<int>(true, string.Empty, msg.Id);
        }

        /// <summary>
        /// Envia uma mensagem para todos os usuários de um perfil, retornando a lista de ids criados.
        /// </summary>
        /// <param name="remetenteId">Identificador opcional do remetente.</param>
        /// <param name="perfilId">Perfil dos destinatários.</param>
        /// <param name="assunto">Assunto compartilhado.</param>
        /// <param name="corpo">Corpo compartilhado.</param>
        /// <param name="mensagemPaiId">Mensagem à qual esta comunicação responde, quando aplicável.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado com os ids das mensagens criadas ou erro contextualizado.</returns>
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

            var ids = new List<int>();
            foreach (var destinatario in usuarios)
            {
                var msg = new Mensagem
                {
                    RemetenteId = remetenteId,
                    DestinatarioId = destinatario.Id,
                    Assunto = assuntoLimpo,
                    Corpo = corpoLimpo,
                    MensagemPaiId = mensagemPaiId,
                    Lida = false
                };
                await _uow.Mensagens.AddAsync(msg, cancellationToken);
                ids.Add(msg.Id);
            }

            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult<List<int>>(true, string.Empty, ids);
        }

        /// <summary>
        /// Marca uma mensagem como lida pelo destinatário, registrando a data de leitura.
        /// </summary>
        /// <param name="id">Identificador da mensagem.</param>
        /// <param name="usuarioId">Usuário que está realizando a leitura.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado indicando sucesso ou erro de autorização/ausência.</returns>
        public async Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default)
        {
            var msg = await _uow.Mensagens.GetByIdAsync(id, cancellationToken);
            if (msg == null || msg.DestinatarioId != usuarioId)
                return new OperationResult(false, "Mensagem não encontrada");
            if (!msg.Lida)
            {
                msg.Lida = true;
                msg.DataLeitura = DateTime.UtcNow;
                _uow.Mensagens.Update(msg);
                await _uow.ConfirmarAsync(cancellationToken);
            }
            return new OperationResult(true, string.Empty);
        }

        /// <summary>
        /// Conta quantas mensagens não lidas existem para o usuário informado.
        /// </summary>
        /// <param name="usuarioId">Identificador do destinatário.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Total de mensagens pendentes de leitura.</returns>
        public async Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default)
        {
            var total = await _uow.Mensagens.Query().CountAsync(m => m.DestinatarioId == usuarioId && !m.Lida, cancellationToken);
            return total;
        }
    }
}
