using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.CORE.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sistema.CORE.Services
{
    public class MensagemService : IMensagemService
    {
        private readonly IUnitOfWork _uow;

        public MensagemService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null)
        {
            var query = _uow.Mensagens.Query()
                .Where(m => m.DestinatarioId == usuarioId)
                .OrderByDescending(m => m.DataInclusao);

            if (remetenteId.HasValue)
                query = query.Where(m => m.RemetenteId == remetenteId.Value);
            if (!string.IsNullOrWhiteSpace(palavraChave))
                query = query.Where(m => m.Assunto.Contains(palavraChave) || m.Corpo.Contains(palavraChave));
            if (inicio.HasValue)
                query = query.Where(m => m.DataInclusao >= inicio.Value);
            if (fim.HasValue)
                query = query.Where(m => m.DataInclusao <= fim.Value);

            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<Mensagem>(items, total, page, pageSize));
        }

        public Task<PagedResult<Mensagem>> BuscarCaixaSaidaAsync(int usuarioId, int page, int pageSize)
        {
            var query = _uow.Mensagens.Query()
                .Where(m => m.RemetenteId == usuarioId)
                .OrderByDescending(m => m.DataInclusao);
            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResult<Mensagem>(items, total, page, pageSize));
        }

        public Task<Mensagem?> BuscarPorIdAsync(int id) => _uow.Mensagens.GetByIdAsync(id);

        public async Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null)
        {
            var msg = new Mensagem
            {
                RemetenteId = remetenteId,
                DestinatarioId = destinatarioId,
                Assunto = assunto,
                Corpo = corpo,
                MensagemPaiId = mensagemPaiId,
                Lida = false
            };
            await _uow.Mensagens.AddAsync(msg);
            await _uow.ConfirmarAsync();
            return OperationResult<int>.Success(msg.Id);
        }

        public async Task<OperationResult> MarcarComoLidaAsync(int id, int usuarioId)
        {
            var msg = await _uow.Mensagens.GetByIdAsync(id);
            if (msg == null || msg.DestinatarioId != usuarioId)
                return OperationResult.Failure("Mensagem não encontrada");
            if (!msg.Lida)
            {
                msg.Lida = true;
                msg.DataLeitura = DateTime.UtcNow;
                _uow.Mensagens.Update(msg);
                await _uow.ConfirmarAsync();
            }
            return OperationResult.Success();
        }
    }
}
