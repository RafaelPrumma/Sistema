using Sistema.CORE.Entities;

namespace Sistema.APP.DTOs;

public class FeedFiltroDto
{
    public PublicacaoTipo? Tipo { get; set; }
    public int? PerfilId { get; set; }
    public bool SomenteNaoLidas { get; set; }
    public AvisoPrioridade? PrioridadeMinima { get; set; }
    public string? PalavraChave { get; set; }
}
