namespace Sistema.APP.DTOs
{
    public class NovaMensagemDto
    {
        public int DestinatarioId { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public int? MensagemPaiId { get; set; }
        public int? PerfilId { get; set; }
    }
}
