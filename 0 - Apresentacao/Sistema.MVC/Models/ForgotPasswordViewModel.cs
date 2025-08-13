using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class ForgotPasswordViewModel
{
    [Required]
    [Display(Name = "CPF")]
    public string Cpf { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
