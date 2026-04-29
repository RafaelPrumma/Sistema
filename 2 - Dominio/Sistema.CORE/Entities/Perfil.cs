namespace Sistema.CORE.Entities;

public class Perfil : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    public void DefinirNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do perfil é obrigatório.", nameof(nome));

        Nome = nome.Trim();
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}
