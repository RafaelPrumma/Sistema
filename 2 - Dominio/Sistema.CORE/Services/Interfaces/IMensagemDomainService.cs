using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Services.Interfaces
{
    /// <summary>
    /// Serviço de mensagens responsável por consultas, envio e atualização de status de leitura.
    /// </summary>
    public interface IMensagemDomainService
    {
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
        Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca mensagens enviadas por um usuário aplicando paginação padrão.
        /// </summary>
        /// <param name="usuarioId">Identificador do remetente.</param>
        /// <param name="page">Página solicitada (base 1).</param>
        /// <param name="pageSize">Quantidade máxima de itens por página.</param>
        /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
        /// <returns>Lista paginada das mensagens enviadas.</returns>
        Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém uma mensagem específica incluindo dados de remetente, destinatário e mensagem pai.
        /// </summary>
        /// <param name="id">Identificador único da mensagem.</param>
        /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
        /// <returns>Instância encontrada ou nula quando inexistente.</returns>
        Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recupera toda a conversa relacionada a uma mensagem desde a origem, validando a participação do usuário.
        /// </summary>
        /// <param name="mensagemId">Mensagem base da consulta.</param>
        /// <param name="usuarioId">Usuário que solicita a conversa (precisa ser participante).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>A mensagem raiz com suas respostas encadeadas ou nulo caso o usuário não tenha acesso.</returns>
        Task<Mensagem?> BuscarConversaAsync(int mensagemId, int usuarioId, CancellationToken cancellationToken = default);

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
        Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default);

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
        Task<OperationResult<List<int>>> EnviarParaPerfilAsync(int? remetenteId, int perfilId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca uma mensagem como lida pelo destinatário, registrando a data de leitura.
        /// </summary>
        /// <param name="id">Identificador da mensagem.</param>
        /// <param name="usuarioId">Usuário que está realizando a leitura.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado indicando sucesso ou erro de autorização/ausência.</returns>
        Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conta quantas mensagens não lidas existem para o usuário informado.
        /// </summary>
        /// <param name="usuarioId">Identificador do destinatário.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Total de mensagens pendentes de leitura.</returns>
        Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default);
    }
}
