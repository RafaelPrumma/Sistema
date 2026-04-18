using System.ComponentModel.DataAnnotations;

namespace Sistema.APP.DTOs;

public class LoginDto
{
    [Required]
    public string Cpf { get; set; } = string.Empty;
    [Required]
    public string Senha { get; set; } = string.Empty;
}
