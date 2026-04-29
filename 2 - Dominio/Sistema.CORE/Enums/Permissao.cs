namespace Sistema.CORE.Enums;

[System.Flags]
public enum Permissao
{
    Nenhuma = 0,
    Visualizar = 1 << 0,
    Criar = 1 << 1,
    Editar = 1 << 2,
    Excluir = 1 << 3,
    Exportar = 1 << 4,
    Aprovar = 1 << 5,
    Administrar = ~0
}
