using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sistema.MVC.Models
{
    public class NovaMensagemViewModel : IValidatableObject
    {
        [Display(Name = "Assunto")]
        public string Assunto { get; set; } = string.Empty;

        [Display(Name = "Mensagem")]
        public string Corpo { get; set; } = string.Empty;

        public int? MensagemPaiId { get; set; }

        [Display(Name = "Destinatários")]
        public List<string> DestinatarioSelecionados { get; set; } = new();

        public IEnumerable<SelectListItem> Destinatarios { get; set; } = new List<SelectListItem>();

        public bool ExibirCampoAssunto => !MensagemPaiId.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExibirCampoAssunto && string.IsNullOrWhiteSpace(Assunto))
            {
                yield return new ValidationResult("Assunto é obrigatório", new[] { nameof(Assunto) });
            }

            if (DestinatarioSelecionados is null || !DestinatarioSelecionados.Any())
            {
                yield return new ValidationResult("Selecione ao menos um destinatário ou setor.", new[] { nameof(DestinatarioSelecionados) });
            }
        }
    }
}
