using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services;

namespace Sistema.CORE.Tests;

public class MensagemServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IMensagemRepository> _mensagemRepoMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();

    public MensagemServiceTests()
    {
        _uowMock.SetupGet(u => u.Mensagens).Returns(_mensagemRepoMock.Object);
        _uowMock.SetupGet(u => u.Usuarios).Returns(_usuarioRepoMock.Object);
    }

    [Theory]
    [InlineData("", "Assunto é obrigatório.")]
    [InlineData("   ", "Assunto é obrigatório.")]
    public async Task EnviarAsync_DeveFalhar_QuandoAssuntoInvalido(string assunto, string mensagemEsperada)
    {
        var service = new MensagemService(_uowMock.Object);

        var resultado = await service.EnviarAsync(1, 2, assunto, "corpo");

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Be(mensagemEsperada);
    }

    [Fact]
    public async Task EnviarAsync_DeveFalhar_QuandoAssuntoMuitoLongo()
    {
        var service = new MensagemService(_uowMock.Object);
        var assunto = new string('a', 201);

        var resultado = await service.EnviarAsync(1, 2, assunto, "corpo");

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Be("Assunto deve ter no máximo 200 caracteres.");
    }

    [Theory]
    [InlineData("", "Corpo da mensagem é obrigatório.")]
    [InlineData("   ", "Corpo da mensagem é obrigatório.")]
    public async Task EnviarAsync_DeveFalhar_QuandoCorpoInvalido(string corpo, string mensagemEsperada)
    {
        var service = new MensagemService(_uowMock.Object);

        var resultado = await service.EnviarAsync(1, 2, "assunto", corpo);

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Be(mensagemEsperada);
    }

    [Fact]
    public async Task EnviarAsync_DeveFalhar_QuandoCorpoMuitoLongo()
    {
        var service = new MensagemService(_uowMock.Object);
        var corpo = new string('a', 5001);

        var resultado = await service.EnviarAsync(1, 2, "assunto", corpo);

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Be("Corpo da mensagem deve ter no máximo 5000 caracteres.");
    }

    [Fact]
    public async Task EnviarAsync_DeveCadastrarMensagem_QuandoDadosValidos()
    {
        var service = new MensagemService(_uowMock.Object);
        var mensagemPersistida = new Mensagem();

        _usuarioRepoMock.Setup(r => r.BuscarPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 2, Nome = "Destinatario" });
        _usuarioRepoMock.Setup(r => r.BuscarPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 1, Nome = "Remetente" });
        _mensagemRepoMock.Setup(r => r.AddAsync(It.IsAny<Mensagem>(), It.IsAny<CancellationToken>()))
            .Callback<Mensagem, CancellationToken>((msg, _) =>
            {
                msg.Id = 10;
                mensagemPersistida = msg;
            })
            .Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.ConfirmarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var resultado = await service.EnviarAsync(1, 2, " Assunto válido ", " Corpo válido ");

        resultado.Success.Should().BeTrue();
        resultado.Data.Should().Be(10);
        mensagemPersistida.Assunto.Should().Be("Assunto válido");
        mensagemPersistida.Corpo.Should().Be("Corpo válido");
        _mensagemRepoMock.Verify(r => r.AddAsync(It.IsAny<Mensagem>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.ConfirmarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
