using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sistema.CORE.Entities; 

namespace Sistema.CORE.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly IConfiguration _config;

    public AuthService(IUnitOfWork uow, IPasswordHasher<Usuario> hasher, IConfiguration config)
    {
        _uow = uow;
        _hasher = hasher;
        _config = config;
    }

    public async Task<string?> AutenticarAsync(string cpf, string senha)
    {
        var usuario = await _uow.Usuarios.BuscarPorCpfAsync(cpf);
        if (usuario is null || !usuario.Ativo) return null;
        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.Failed) return null;

        var reivindicacoes = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("perfil", usuario.PerfilId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: reivindicacoes,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
