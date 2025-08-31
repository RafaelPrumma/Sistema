using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.CORE.Common;
using Sistema.INFRA.Data;
using System.Linq;
using System.Threading;

namespace Sistema.INFRA.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
        return usuario;
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios.FindAsync(new object?[] { id }, cancellationToken);
        if (usuario is null) return;
        _context.Usuarios.Remove(usuario);
    }

    public async Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Usuarios.AsNoTracking().OrderBy(u => u.Id);
        return await query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<Usuario>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Usuarios.AsQueryable();
        if (inicio.HasValue)
            query = query.Where(u => u.DataInclusao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(u => u.DataInclusao <= fim.Value);
        if (perfilId.HasValue)
            query = query.Where(u => u.PerfilId == perfilId.Value);
        if (ativo.HasValue)
            query = query.Where(u => u.Ativo == ativo.Value);
        query = query.AsNoTracking().OrderBy(u => u.Id);
        return await query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public async Task<bool> ExisteAtivoPorPerfilAsync(int perfilId, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios.AnyAsync(u => u.PerfilId == perfilId && u.Ativo, cancellationToken);
    }

    public async Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Cpf == cpf, cancellationToken);
    }

    public async Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios.FindAsync(new object?[] { id }, cancellationToken);
    }

    public Task AtualizarAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        return Task.CompletedTask;
    }
}
