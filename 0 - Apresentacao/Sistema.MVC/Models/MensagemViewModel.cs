using System;
using System.Collections.Generic;
using Sistema.APP.DTOs;
using Sistema.CORE.Entities;

namespace Sistema.MVC.Models
{
    public class MensagemViewModel
    {
        public List<MensagemDto> Mensagens { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int? RemetenteId { get; set; }
        public string? PalavraChave { get; set; }
        public DateTime? Inicio { get; set; }
        public DateTime? Fim { get; set; }
        public PublicacaoTipo? Tipo { get; set; }
        public int? PerfilId { get; set; }
        public bool SomenteNaoLidas { get; set; }
        public AvisoPrioridade? PrioridadeMinima { get; set; }
    }
}
