using Sistema.CORE.Entities;

namespace Sistema.APP.DTOs
{
    public class NovaMensagemDto
    {
        public PublicacaoTipo Tipo { get; set; } = PublicacaoTipo.MensagemDireta;
        public int? DestinatarioId { get; set; }
        public List<int> DestinatariosIds { get; set; } = new();
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public int? MensagemPaiId { get; set; }
        public int? PerfilId { get; set; }
        public AvisoAudiencia? AvisoAudiencia { get; set; }
        public AvisoPrioridade? AvisoPrioridade { get; set; }
        public DateTime? AvisoValidoAte { get; set; }
        public bool Fixada { get; set; }
    }
}
