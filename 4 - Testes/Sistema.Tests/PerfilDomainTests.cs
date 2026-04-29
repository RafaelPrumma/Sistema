using Sistema.CORE.Entities;

namespace Sistema.Tests;

public class PerfilDomainTests
{
    [Fact]
    public void DefinirNomeDeveLancarQuandoNomeInvalido()
    {
        var perfil = new Perfil();
        Assert.Throws<ArgumentException>(() => perfil.DefinirNome("  "));
    }

    [Fact]
    public void DefinirNomeDeveAplicarTrim()
    {
        var perfil = new Perfil();
        perfil.DefinirNome("  Financeiro  ");
        Assert.Equal("Financeiro", perfil.Nome);
    }

    [Fact]
    public void AtivarEDesativarDeveAlternarEstado()
    {
        var perfil = new Perfil();
        perfil.Desativar();
        Assert.False(perfil.Ativo);
        perfil.Ativar();
        Assert.True(perfil.Ativo);
    }
}
