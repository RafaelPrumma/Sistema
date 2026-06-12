namespace Sistema.APP.Services.Interfaces;

public interface IFinancasImportador
{
    Task GarantirCargaInicialAsync(CancellationToken cancellationToken = default);
    Task ImportarPastaMonitoradaAsync(CancellationToken cancellationToken = default);
}
