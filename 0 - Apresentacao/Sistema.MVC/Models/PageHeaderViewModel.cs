using System.ComponentModel.DataAnnotations;

namespace Sistema.MVC.Models;

public class PageHeaderViewModel
{
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
