namespace Sistema.APP.Services.Interfaces;

public interface IFinancasImportador
{
    Task GarantirCargaInicialAsync(CancellationToken cancellationToken = default);

    /// <summary>Varre as pastas monitoradas e devolve quantos arquivos NOVOS foram importados.</summary>
    Task<int> ImportarPastaMonitoradaAsync(CancellationToken cancellationToken = default);
}
