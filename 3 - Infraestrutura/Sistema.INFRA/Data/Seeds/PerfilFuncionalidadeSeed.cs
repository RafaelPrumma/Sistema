using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class PerfilFuncionalidadeSeed
{
    public static IEnumerable<PerfilFuncionalidade> Get()
    {
        return new List<PerfilFuncionalidade>
        {
            new() { PerfilId = 1, FuncionalidadeId = 1, PodeLer = true, PodeEscrever = true },
            new() { PerfilId = 1, FuncionalidadeId = 2, PodeLer = true, PodeEscrever = true },
            new() { PerfilId = 1, FuncionalidadeId = 3, PodeLer = true, PodeEscrever = true },
            new() { PerfilId = 2, FuncionalidadeId = 1, PodeLer = true, PodeEscrever = false },
            new() { PerfilId = 2, FuncionalidadeId = 2, PodeLer = true, PodeEscrever = true }
        };
    }
}
