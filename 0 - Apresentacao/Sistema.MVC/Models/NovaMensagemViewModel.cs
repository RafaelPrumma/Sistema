using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema.CORE.Entities;

namespace Sistema.MVC.Models
{
    public class NovaMensagemViewModel : IValidatableObject
    {
        [Display(Name = "Tipo de publicação")]
        public PublicacaoTipo Tipo { get; set; } = PublicacaoTipo.MensagemDireta;

        [Display(Name = "Assunto")]
        public string Assunto { get; set; } = string.Empty;

        [Display(Name = "Mensagem")]
        public string Corpo { get; set; } = string.Empty;

        public int? MensagemPaiId { get; set; }

        [Display(Name = "Destinatários")]
        public List<string> DestinatarioSelecionados { get; set; } = new();

        public IEnumerable<SelectListItem> Destinatarios { get; set; } = new List<SelectListItem>();

        [Display(Name = "Setor")]
        public int? PerfilId { get; set; }

        public IEnumerable<SelectListItem> Perfis { get; set; } = new List<SelectListItem>();

        [Display(Name = "Audiência do aviso")]
        public AvisoAudiencia? AvisoAudiencia { get; set; }

        [Display(Name = "Prioridade")]
        public AvisoPrioridade? AvisoPrioridade { get; set; }

        [Display(Name = "Válido até")]
        public DateTime? AvisoValidoAte { get; set; }

        [Display(Name = "Fixar no topo")]
        public bool Fixada { get; set; }

        public bool ExibirCampoAssunto => !MensagemPaiId.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExibirCampoAssunto && string.IsNullOrWhiteSpace(Assunto))
            {
                yield return new ValidationResult("Assunto é obrigatório", new[] { nameof(Assunto) });
            }

            if (Tipo == PublicacaoTipo.MensagemDireta && (DestinatarioSelecionados is null || DestinatarioSelecionados.Count == 0))
            {
                yield return new ValidationResult("Selecione ao menos um destinatário ou setor.", new[] { nameof(DestinatarioSelecionados) });
            }

            if (Tipo == PublicacaoTipo.PostSetor && !PerfilId.HasValue)
            {
                yield return new ValidationResult("Selecione o setor para o post.", new[] { nameof(PerfilId) });
            }

            if (Tipo == PublicacaoTipo.Aviso && !AvisoAudiencia.HasValue)
            {
                yield return new ValidationResult("Selecione a audiência do aviso.", new[] { nameof(AvisoAudiencia) });
            }
        }
    }
}
