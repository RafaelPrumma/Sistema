using System;
using System.Collections.Generic;

namespace Sistema.MVC.Models
{
    public class DashboardViewModel
    {
        public int TotalUsuarios { get; set; }
        public int TotalPerfis { get; set; }
        public int TotalFuncionalidades { get; set; }
        public int TotalConfiguracoes { get; set; }
        public int TotalMensagens { get; set; }
        public int TotalMensagensNaoLidas { get; set; }
        public List<DashboardMensagemViewModel> MensagensRecentes { get; set; } = [];
        public List<DashboardAtalhoViewModel> Atalhos { get; set; } = [];
    }

    public class DashboardMensagemViewModel
    {
        public int Id { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public DateTime DataInclusao { get; set; }
    }

    public class DashboardAtalhoViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Icone { get; set; } = "bi-circle";
        public string Controller { get; set; } = "Home";
        public string Action { get; set; } = "Index";
        public string Variante { get; set; } = "primary";
    }
}
