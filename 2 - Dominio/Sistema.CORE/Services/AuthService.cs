using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

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

    public async Task<string?> AuthenticateAsync(string cpf, string senha)
    {
        var user = await _uow.Usuarios.GetByCpfAsync(cpf);
        if (user is null || !user.Ativo) return null;
        var result = _hasher.VerifyHashedPassword(user, user.SenhaHash, senha);
        if (result == PasswordVerificationResult.Failed) return null;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Nome),
            new Claim("perfil", user.PerfilId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
