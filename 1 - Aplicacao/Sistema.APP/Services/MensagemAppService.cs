using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class MensagemAppService(Sistema.CORE.Services.Interfaces.IMensagemService domainService) : IMensagemService
{
    private readonly Sistema.CORE.Services.Interfaces.IMensagemService _domainService = domainService;

    public Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default) =>
        _domainService.BuscarCaixaEntradaAsync(usuarioId, page, pageSize, remetenteId, palavraChave, inicio, fim, cancellationToken);

    public Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _domainService.BuscarCaixaSaidaAsync(usuarioId, page, pageSize, cancellationToken);

    public Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorIdAsync(id, cancellationToken);

    public Task<Mensagem?> BuscarConversaAsync(int mensagemId, int usuarioId, CancellationToken cancellationToken = default) =>
        _domainService.BuscarConversaAsync(mensagemId, usuarioId, cancellationToken);

    public Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default) =>
        _domainService.EnviarAsync(remetenteId, destinatarioId, assunto, corpo, mensagemPaiId, cancellationToken);

    public Task<OperationResult<List<int>>> EnviarParaPerfilAsync(int? remetenteId, int perfilId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default) =>
        _domainService.EnviarParaPerfilAsync(remetenteId, perfilId, assunto, corpo, mensagemPaiId, cancellationToken);

    public Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId, CancellationToken cancellationToken = default) =>
        _domainService.MarcarComoLidaAsync(id, usuarioId, cancellationToken);

    public Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        _domainService.ContarNaoLidasAsync(usuarioId, cancellationToken);
}
