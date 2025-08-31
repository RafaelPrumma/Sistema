using System;

namespace Sistema.APP.DTOs
{
    public class MensagemDto
    {
        public int Id { get; set; }
        public int? RemetenteId { get; set; }
        public string RemetenteNome { get; set; } = string.Empty;
        public int DestinatarioId { get; set; }
        public string DestinatarioNome { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public DateTime DataInclusao { get; set; }
        public DateTime? DataLeitura { get; set; }
        public int? MensagemPaiId { get; set; }
    }
}
