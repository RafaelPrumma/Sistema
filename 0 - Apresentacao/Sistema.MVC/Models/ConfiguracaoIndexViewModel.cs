using System.Collections.Generic;

namespace Sistema.MVC.Models;

public class ConfiguracaoIndexViewModel
{
    public string Agrupamento { get; set; } = string.Empty;
    public List<ConfiguracaoViewModel> Configuracoes { get; set; } = new();
}
