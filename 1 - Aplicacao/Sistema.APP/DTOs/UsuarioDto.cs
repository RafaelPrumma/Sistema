namespace Sistema.APP.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public string SenhaHash { get; set; } = string.Empty;
    public int PerfilId { get; set; }
}
