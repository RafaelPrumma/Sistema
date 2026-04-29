using Sistema.CORE.Entities;
using Sistema.CORE.Enums;

namespace Sistema.INFRA.Data.Seeds;

public static class PerfilFuncionalidadeSeed
{
    public static IEnumerable<PerfilFuncionalidade> Get()
    {
        return new List<PerfilFuncionalidade>
        {
            new() { PerfilId = 1, FuncionalidadeId = 1, Permissoes = Permissao.Administrar },
            new() { PerfilId = 1, FuncionalidadeId = 2, Permissoes = Permissao.Administrar },
            new() { PerfilId = 1, FuncionalidadeId = 3, Permissoes = Permissao.Administrar },
            new() { PerfilId = 1, FuncionalidadeId = 4, Permissoes = Permissao.Administrar },
            new() { PerfilId = 1, FuncionalidadeId = 5, Permissoes = Permissao.Administrar },
            new() { PerfilId = 1, FuncionalidadeId = 6, Permissoes = Permissao.Administrar },
            new() { PerfilId = 2, FuncionalidadeId = 1, Permissoes = Permissao.Visualizar },
            new() { PerfilId = 2, FuncionalidadeId = 2, Permissoes = Permissao.Visualizar | Permissao.Criar | Permissao.Editar },
            new() { PerfilId = 2, FuncionalidadeId = 4, Permissoes = Permissao.Visualizar | Permissao.Criar },
            new() { PerfilId = 2, FuncionalidadeId = 6, Permissoes = Permissao.Visualizar }
        };
    }
}
