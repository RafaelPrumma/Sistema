namespace Sistema.APP.Services.Interfaces;

public interface IMinhasFinancasImportador
{
    Task GarantirCargaInicialAsync(CancellationToken cancellationToken = default);
    Task ImportarPastaMonitoradaAsync(CancellationToken cancellationToken = default);
}
