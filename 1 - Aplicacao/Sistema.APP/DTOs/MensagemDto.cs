using System;
using System.Collections.Generic;
using Sistema.CORE.Entities;

namespace Sistema.APP.DTOs
{
    public class MensagemDto
    {
        public int Id { get; set; }
        public PublicacaoTipo Tipo { get; set; }
        public PublicacaoStatus Status { get; set; }
        public int? AutorId { get; set; }
        public int? RemetenteId { get; set; }
        public string RemetenteNome { get; set; } = string.Empty;
        public int? DestinatarioId { get; set; }
        public string DestinatarioNome { get; set; } = string.Empty;
        public int? PerfilId { get; set; }
        public string PerfilNome { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public DateTime DataInclusao { get; set; }
        public DateTime? DataLeitura { get; set; }
        public int? MensagemPaiId { get; set; }
        public AvisoAudiencia? AvisoAudiencia { get; set; }
        public AvisoPrioridade? AvisoPrioridade { get; set; }
        public DateTime? AvisoValidoAte { get; set; }
        public bool Fixada { get; set; }
        public Dictionary<TipoReacao, int> Reacoes { get; set; } = new();
        public bool PodeResponder { get; set; }
        public bool PodeReagir { get; set; }
        public bool PodeModerar { get; set; }
    }
}
