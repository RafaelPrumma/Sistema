using System.ComponentModel.DataAnnotations;
using Sistema.CORE.Entities;

namespace Sistema.MVC.Models;

public class ConfiguracaoViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O agrupamento é obrigatório")]
    public string Agrupamento { get; set; } = string.Empty;

    [Required(ErrorMessage = "A chave é obrigatória")]
    public string Chave { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public ConfiguracaoTipo Tipo { get; set; }

    public string? Descricao { get; set; }

    [Display(Name = "Ativo")]
    public bool Ativo { get; set; } = true;
}
