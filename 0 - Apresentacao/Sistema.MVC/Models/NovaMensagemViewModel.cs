using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sistema.MVC.Models
{
    public class NovaMensagemViewModel
    {
        [Required(ErrorMessage = "Assunto é obrigatório")]
        [Display(Name = "Assunto")]
        public string Assunto { get; set; } = string.Empty;

        [Display(Name = "Mensagem")]
        public string Corpo { get; set; } = string.Empty;

        public int? MensagemPaiId { get; set; }

        [Display(Name = "Destinatário")]
        public int? DestinatarioId { get; set; }

        [Display(Name = "Enviar para setor")]
        public int? PerfilId { get; set; }

        public IEnumerable<SelectListItem> Destinatarios { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Perfis { get; set; } = new List<SelectListItem>();
    }
}
