namespace Sistema.CORE.Entities;

public class MensagemDestinatario
{
    public int MensagemId { get; set; }
    public Mensagem Mensagem { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
