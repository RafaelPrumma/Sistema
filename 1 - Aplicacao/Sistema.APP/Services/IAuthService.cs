using System.Threading.Tasks;

namespace Sistema.CORE.Services;

public interface IAuthService
{
    Task<string?> AutenticarAsync(string cpf, string senha);
}
