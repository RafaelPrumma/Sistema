namespace Sistema.MVC.Models;

public class FilaViewModel
{
    public bool HangfireDisponivel { get; set; }
    public long Enfileirados { get; set; }
    public long Processando { get; set; }
    public long Sucesso { get; set; }
    public long Falhas { get; set; }
    public long Agendados { get; set; }
    public long Recorrentes { get; set; }

    public List<FilaRecorrenteItem> JobsRecorrentes { get; set; } = new();
    public List<FilaJobItem> UltimasFalhas { get; set; } = new();
    public List<FilaJobItem> UltimosSucessos { get; set; } = new();
}

public record FilaRecorrenteItem(string Id, string Cron, DateTime? UltimaExecucao, DateTime? ProximaExecucao, string? UltimoEstado);

public record FilaJobItem(string Id, string Job, string? Info, DateTime? Quando);
