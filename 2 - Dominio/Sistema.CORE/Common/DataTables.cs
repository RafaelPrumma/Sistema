namespace Sistema.CORE.Common;

using Microsoft.EntityFrameworkCore;

// Contrato server-side do DataTables (payload achatado pelo datatable.js: o cliente manda
// orderColumn/orderDir/search e o servidor devolve apenas uma página, com os totais).
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public string? Search { get; set; }
    public string? OrderColumn { get; set; }
    public string OrderDir { get; set; } = "asc";
}

public class DataTablesResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = [];

    // Reprojeta a página (entidade → DTO) mantendo os totais e o draw.
    public DataTablesResponse<TOut> Map<TOut>(Func<T, TOut> seletor) => new()
    {
        Draw = Draw,
        RecordsTotal = RecordsTotal,
        RecordsFiltered = RecordsFiltered,
        Data = Data.Select(seletor).ToList()
    };
}

public static class DataTablesExtensions
{
    // Aplica busca global (opcional), ordenação por coluna (mapa type-safe) e paginação,
    // devolvendo só a página pedida + os totais (sem e com filtro).
    public static async Task<DataTablesResponse<T>> ToDataTablesAsync<T>(
        this IQueryable<T> query,
        DataTablesRequest request,
        Func<IQueryable<T>, string, IQueryable<T>>? aplicarBusca,
        IReadOnlyDictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> ordenacoes,
        string ordemPadrao,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);

        var filtrada = query;
        if (aplicarBusca is not null && !string.IsNullOrWhiteSpace(request.Search))
            filtrada = aplicarBusca(query, request.Search.Trim());

        var totalFiltrado = await filtrada.CountAsync(cancellationToken);

        var desc = string.Equals(request.OrderDir, "desc", StringComparison.OrdinalIgnoreCase);
        var chave = !string.IsNullOrWhiteSpace(request.OrderColumn) && ordenacoes.ContainsKey(request.OrderColumn!)
            ? request.OrderColumn!
            : ordemPadrao;

        var ordenada = ordenacoes.TryGetValue(chave, out var aplicarOrdem)
            ? aplicarOrdem(filtrada, desc)
            : filtrada.OrderBy(_ => 0);

        var start = request.Start < 0 ? 0 : request.Start;
        var length = request.Length is < 1 or > 200 ? 10 : request.Length;

        var data = await ordenada.Skip(start).Take(length).ToListAsync(cancellationToken);

        return new DataTablesResponse<T>
        {
            Draw = request.Draw,
            RecordsTotal = total,
            RecordsFiltered = totalFiltrado,
            Data = data
        };
    }
}
