using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Senha Atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Nova Senha")]
    [MinLength(6, ErrorMessage = "A nova senha deve ter pelo menos 6 caracteres")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Senha")]
    [Compare("NovaSenha", ErrorMessage = "As senhas não conferem")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}
