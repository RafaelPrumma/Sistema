using System;

namespace Sistema.CORE.Entities
{
    public class Mensagem : AuditableEntity
    {
        public int Id { get; set; }
        public int? RemetenteId { get; set; }
        public Usuario? Remetente { get; set; }
        public int DestinatarioId { get; set; }
        public Usuario Destinatario { get; set; } = null!;
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public DateTime? DataLeitura { get; set; }
        public int? MensagemPaiId { get; set; }
        public Mensagem? MensagemPai { get; set; }
    }
}
