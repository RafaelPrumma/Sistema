using Sistema.CORE.Entities;

namespace Sistema.APP.DTOs;

public class ConfiguracaoDto
{
	public int Id { get; set; }
	public string Agrupamento { get; set; } = string.Empty;
	public string Chave { get; set; } = string.Empty;
	public string Valor { get; set; } = string.Empty;
	public ConfiguracaoTipo Tipo { get; set; }
	public string? Descricao { get; set; }
	public bool Ativo { get; set; } = true;
}