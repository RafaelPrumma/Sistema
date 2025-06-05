namespace Sistema.APP.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public string? Senha { get; set; }
    public bool Ativo { get; set; }
}
