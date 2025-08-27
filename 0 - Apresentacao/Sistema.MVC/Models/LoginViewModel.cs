using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "O CPF é obrigatório")]
    [Display(Name = "CPF")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;
}
