using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Nova Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Senha")]
    [Compare("Senha", ErrorMessage = "As senhas não conferem")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}
