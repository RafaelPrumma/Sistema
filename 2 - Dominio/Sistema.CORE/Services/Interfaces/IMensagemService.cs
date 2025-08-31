using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using System;
using System.Threading.Tasks;

namespace Sistema.CORE.Services.Interfaces
{
    public interface IMensagemService
    {
        Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null);
        Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize);
        Task<Mensagem?> BuscarPorIdAsync(int id);
        Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null);
        Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId);
    }
}
