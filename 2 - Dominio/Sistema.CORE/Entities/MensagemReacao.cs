namespace Sistema.CORE.Entities;

public class MensagemReacao
{
    public int Id { get; set; }
    public int PublicacaoId { get; set; }
    public Mensagem Publicacao { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public TipoReacao TipoReacao { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
}
