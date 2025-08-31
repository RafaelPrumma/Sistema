using System.Threading;
using System.Threading.Tasks;

namespace Sistema.CORE.Interfaces;

public interface IAuthService
{
    Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default);
}
