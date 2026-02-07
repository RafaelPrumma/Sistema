using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services.Interfaces;

public interface IMensagemService
{
    Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Mensagem?> BuscarConversaAsync(int mensagemId, int usuarioId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default);
    Task<OperationResult<List<int>>> EnviarParaPerfilAsync(int? remetenteId, int perfilId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default);
    Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default);
    Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default);
}
