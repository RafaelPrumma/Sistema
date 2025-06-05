using System.Threading.Tasks;

namespace Sistema.CORE.Services;

public interface IAuthService
{
    Task<string?> AuthenticateAsync(string cpf, string senha);
}
