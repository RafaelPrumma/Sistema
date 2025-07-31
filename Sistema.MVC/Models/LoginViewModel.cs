using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "CPF")]
    public string Cpf { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;
}
