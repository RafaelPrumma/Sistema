using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services;

/// <summary>
/// Serviço responsável por operações de usuário e auditoria das ações executadas.
/// </summary>
public class UsuarioService(IUnitOfWork uow, ILogService log) : IUsuarioService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ILogService _log = log;

    /// <summary>
    /// Recupera todos os usuários ativos aplicando paginação.
    /// </summary>
    /// <param name="page">Página solicitada (base 1).</param>
    /// <param name="pageSize">Quantidade de registros por página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado paginado de usuários.</returns>
    public Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _uow.Usuarios.BuscarTodosAsync(page, pageSize, cancellationToken);

    /// <summary>
    /// Busca um usuário pelo identificador único.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário encontrado ou nulo.</returns>
    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorIdAsync(id, cancellationToken);

    /// <summary>
    /// Busca um usuário pelo CPF.
    /// </summary>
    /// <param name="cpf">CPF do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário localizado ou nulo.</returns>
    public Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorCpfAsync(cpf, cancellationToken);

    /// <summary>
    /// Localiza usuário pelo token de redefinição de senha.
    /// </summary>
    /// <param name="token">Token emitido para recuperação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário correspondente ou nulo.</returns>
    public Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorResetTokenAsync(token, cancellationToken);

    /// <summary>
    /// Adiciona um novo usuário, registrando logs de sucesso ou conflito de CPF.
    /// </summary>
    /// <param name="usuario">Entidade a ser criada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo sucesso e a entidade criada quando aplicável.</returns>
    public async Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf, cancellationToken);
        if (existing is not null)
        {
            await _log.RegistrarAsync(nameof(Usuario), "Add", false, "Usuário já existe", LogTipo.Erro, usuario.UsuarioInclusao, null, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult<Usuario>(false, "Usuário já existe");
        }

        var created = await _uow.Usuarios.AdicionarAsync(usuario, cancellationToken);
        await _log.RegistrarAsync(nameof(Usuario), "Add", true, "Usuário criado", LogTipo.Sucesso, usuario.UsuarioInclusao, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<Usuario>(true, "Usuário criado com sucesso", created);
    }

    /// <summary>
    /// Atualiza dados de um usuário e registra o resultado da operação no log.
    /// </summary>
    /// <param name="usuario">Usuário com informações atualizadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado sinalizando sucesso ou conflito de CPF.</returns>
    public async Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf, cancellationToken);
        if (existing is not null && existing.Id != usuario.Id)
        {
            await _log.RegistrarAsync(nameof(Usuario), "Update", false, "CPF já utilizado", LogTipo.Erro, usuario.UsuarioAlteracao ?? "system", null, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult(false, "CPF já utilizado");
        }

        await _uow.Usuarios.AtualizarAsync(usuario);
        await _log.RegistrarAsync(nameof(Usuario), "Update", true, "Usuário atualizado", LogTipo.Sucesso, usuario.UsuarioAlteracao ?? "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Usuário atualizado com sucesso");
    }

    /// <summary>
    /// Remove um usuário pelo identificador e registra o evento de exclusão.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Usuarios.RemoverAsync(id, cancellationToken);
        await _log.RegistrarAsync(nameof(Usuario), "Delete", true, "Usuário removido", LogTipo.Sucesso, "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Usuário removido");
    }
}
