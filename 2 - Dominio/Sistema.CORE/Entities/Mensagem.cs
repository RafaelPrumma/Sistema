using System;

namespace Sistema.CORE.Entities
{
    public class Mensagem : AuditableEntity
    {
        public int Id { get; set; }
        public PublicacaoTipo Tipo { get; set; } = PublicacaoTipo.MensagemDireta;
        public PublicacaoStatus Status { get; set; } = PublicacaoStatus.Ativa;
        public int? AutorId { get; set; }
        public Usuario? Autor { get; set; }
        public int? RemetenteId { get; set; }
        public Usuario? Remetente { get; set; }
        public int? DestinatarioId { get; set; }
        public Usuario? Destinatario { get; set; }
        public int? PerfilId { get; set; }
        public Perfil? Perfil { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public DateTime? DataLeitura { get; set; }
        public int? MensagemPaiId { get; set; }
        public Mensagem? MensagemPai { get; set; }
        public List<Mensagem> Respostas { get; set; } = new();

        public AvisoAudiencia? AvisoAudiencia { get; set; }
        public AvisoPrioridade? AvisoPrioridade { get; set; }
        public DateTime? AvisoValidoAte { get; set; }
        public bool Fixada { get; set; }
        public string? AvisoGrupo { get; set; }

        public List<MensagemDestinatario> DestinatariosExplicitos { get; set; } = new();
        public List<MensagemReacao> Reacoes { get; set; } = new();
        public List<MensagemLeitura> Leituras { get; set; } = new();
    }
}
