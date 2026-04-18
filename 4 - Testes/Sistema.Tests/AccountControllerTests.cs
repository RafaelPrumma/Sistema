using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.MVC.Controllers;
using Sistema.MVC.Models;

namespace Sistema.Tests;

public class AccountControllerTests
{
    [Fact]
    public async Task RegisterDeveRetornarOkQuandoCadastroForSucesso()
    {
        var usuarioService = new Mock<IUsuarioAppService>();
        var hasher = new Mock<IPasswordHasher<Usuario>>();
        var emailService = new Mock<IEmailAppService>();
        var logger = new Mock<ILogger<AccountController>>();
        var logService = new Mock<ILogAppService>();
        var uow = new Mock<IUnitOfWork>();

        hasher
            .Setup(h => h.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
            .Returns("senha-hash");

        usuarioService
            .Setup(s => s.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<Usuario>(true, "ok"));

        var controller = new AccountController(usuarioService.Object, hasher.Object, emailService.Object, logger.Object, logService.Object, uow.Object);
        var model = new RegisterViewModel
        {
            Nome = "Usuário Teste",
            Cpf = "12345678900",
            Email = "teste@exemplo.com",
            Senha = "Senha@123"
        };

        var resultado = await controller.Register(model);

        Assert.IsType<OkObjectResult>(resultado);
        emailService.Verify(
            s => s.EnviarAsync(model.Email, "Bem-vindo", "Seu cadastro foi realizado com sucesso.", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterDeveRetornar500QuandoOcorrerExcecao()
    {
        var usuarioService = new Mock<IUsuarioAppService>();
        var hasher = new Mock<IPasswordHasher<Usuario>>();
        var emailService = new Mock<IEmailAppService>();
        var logger = new Mock<ILogger<AccountController>>();
        var logService = new Mock<ILogAppService>();
        var uow = new Mock<IUnitOfWork>();

        hasher
            .Setup(h => h.HashPassword(It.IsAny<Usuario>(), It.IsAny<string>()))
            .Returns("senha-hash");

        usuarioService
            .Setup(s => s.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada"));

        var controller = new AccountController(usuarioService.Object, hasher.Object, emailService.Object, logger.Object, logService.Object, uow.Object);
        var model = new RegisterViewModel
        {
            Nome = "Usuário Teste",
            Cpf = "12345678900",
            Email = "teste@exemplo.com",
            Senha = "Senha@123"
        };

        var resultado = await controller.Register(model);

        var objectResult = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
