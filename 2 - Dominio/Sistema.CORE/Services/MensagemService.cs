using System;
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
    public class MensagemService : IMensagemService
    {
        private readonly IUnitOfWork _uow;

        public MensagemService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PagedResult<Mensagem>> BuscarCaixaEntradaAsync(int usuarioId, int page, int pageSize, int? remetenteId = null, string? palavraChave = null, DateTime? inicio = null, DateTime? fim = null, CancellationToken cancellationToken = default)
        {
            IQueryable<Mensagem> query = _uow.Mensagens.Query()
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
                .Where(m => m.RemetenteId == usuarioId)
                .OrderByDescending(m => m.DataInclusao);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResult<Mensagem>(items, total, page, pageSize);
        }

        public Task<Mensagem?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Mensagens.GetByIdAsync(id, cancellationToken);

        public async Task<OperationResult<int>> EnviarAsync(int? remetenteId, int destinatarioId, string assunto, string corpo, int? mensagemPaiId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(assunto))
                return new OperationResult<int>(false, "Assunto é obrigatório.");
            if (assunto.Length > 200)
                return new OperationResult<int>(false, "Assunto deve ter no máximo 200 caracteres.");
            if (string.IsNullOrWhiteSpace(corpo))
                return new OperationResult<int>(false, "Corpo da mensagem é obrigatório.");
            if (corpo.Length > 5000)
                return new OperationResult<int>(false, "Corpo da mensagem deve ter no máximo 5000 caracteres.");

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

        public async Task<int> ContarNaoLidasAsync(int usuarioId, CancellationToken cancellationToken = default)
        {
            var total = await _uow.Mensagens.Query()
                .CountAsync(m => m.DestinatarioId == usuarioId && !m.Lida, cancellationToken);
            return total;
        }
    }
}
