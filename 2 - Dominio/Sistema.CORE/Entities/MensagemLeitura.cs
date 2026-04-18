namespace Sistema.CORE.Entities;

public class MensagemLeitura
{
    public int Id { get; set; }
    public int PublicacaoId { get; set; }
    public Mensagem Publicacao { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime DataLeitura { get; set; } = DateTime.UtcNow;
    public DateTime? DataEntrega { get; set; }
}
