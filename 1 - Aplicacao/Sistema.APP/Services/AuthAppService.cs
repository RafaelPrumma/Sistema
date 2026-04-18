using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sistema.APP.Services;

public class AuthAppService(IUnitOfWork uow, IPasswordHasher<Usuario> hasher, IConfiguration config, ILogAppService log) : IAuthAppService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IPasswordHasher<Usuario> _hasher = hasher;
    private readonly IConfiguration _config = config;
    private readonly ILogAppService _log = log;

    public async Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await _uow.Usuarios.BuscarPorCpfAsync(cpf, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            await _log.RegistrarAcessoAsync(nameof(Usuario), "Login", false, "Tentativa de login inválida", LogTipo.Informacao, cpf, "Usuário não encontrado ou inativo", cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            await _log.RegistrarAcessoAsync(nameof(Usuario), "Login", false, "Tentativa de login inválida", LogTipo.Informacao, usuario.Cpf, "Senha inválida", cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return null;
        }

        var reivindicacoes = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("perfil", usuario.PerfilId.ToString(CultureInfo.InvariantCulture))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: reivindicacoes,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        await _log.RegistrarAcessoAsync(nameof(Usuario), "Login", true, "Login realizado com sucesso", LogTipo.Sucesso, usuario.Cpf, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
