using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Sistema.APP.Services;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.APP.Services.Interfaces;

namespace Sistema.Tests;

public class AuthAppServiceTests
{
    [Fact]
    public async Task AutenticarAsyncDeveRetornarTokenQuandoCredenciaisForemValidas()
    {
        var usuarioRepository = new Mock<IUsuarioRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var hasher = new Mock<IPasswordHasher<Usuario>>();
        var logService = new Mock<ILogAppService>();

        var usuario = new Usuario
        {
            Id = 10,
            Nome = "Admin",
            Cpf = "12345678900",
            PerfilId = 1,
            Ativo = true,
            SenhaHash = "hash"
        };

        usuarioRepository
            .Setup(r => r.BuscarPorCpfAsync(usuario.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        unitOfWork.SetupGet(u => u.Usuarios).Returns(usuarioRepository.Object);
        hasher
            .Setup(h => h.VerifyHashedPassword(usuario, usuario.SenhaHash, "Senha@123"))
            .Returns(PasswordVerificationResult.Success);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-super-secreta-com-minimo-32-caracteres",
                ["Jwt:Issuer"] = "Sistema",
                ["Jwt:Audience"] = "Sistema.Web"
            })
            .Build();

        var service = new AuthAppService(unitOfWork.Object, hasher.Object, config, logService.Object);

        var token = await service.AutenticarAsync(usuario.Cpf, "Senha@123");

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task AutenticarAsyncDeveRetornarNullQuandoSenhaForInvalida()
    {
        var usuarioRepository = new Mock<IUsuarioRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var hasher = new Mock<IPasswordHasher<Usuario>>();
        var logService = new Mock<ILogAppService>();

        var usuario = new Usuario
        {
            Id = 11,
            Nome = "Usuario",
            Cpf = "98765432100",
            PerfilId = 2,
            Ativo = true,
            SenhaHash = "hash"
        };

        usuarioRepository
            .Setup(r => r.BuscarPorCpfAsync(usuario.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        unitOfWork.SetupGet(u => u.Usuarios).Returns(usuarioRepository.Object);
        hasher
            .Setup(h => h.VerifyHashedPassword(usuario, usuario.SenhaHash, "senha-invalida"))
            .Returns(PasswordVerificationResult.Failed);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-super-secreta-com-minimo-32-caracteres",
                ["Jwt:Issuer"] = "Sistema",
                ["Jwt:Audience"] = "Sistema.Web"
            })
            .Build();

        var service = new AuthAppService(unitOfWork.Object, hasher.Object, config, logService.Object);

        var token = await service.AutenticarAsync(usuario.Cpf, "senha-invalida");

        Assert.Null(token);
    }
}
