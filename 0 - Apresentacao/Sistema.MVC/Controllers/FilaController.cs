using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;
using Sistema.MVC.Models;

namespace Sistema.MVC.Controllers;

// Fila de jobs do Hangfire no tema do app (alternativa ao painel cru /jobs).
[AuthorizePermission("Log", Permissao.Visualizar)]
public class FilaController : Controller
{
    [HttpGet("/Fila")]
    public IActionResult Index()
    {
        var model = new FilaViewModel();
        try
        {
            var monitor = JobStorage.Current.GetMonitoringApi();
            var stats = monitor.GetStatistics();
            model.HangfireDisponivel = true;
            model.Enfileirados = stats.Enqueued;
            model.Processando = stats.Processing;
            model.Sucesso = stats.Succeeded;
            model.Falhas = stats.Failed;
            model.Agendados = stats.Scheduled;
            model.Recorrentes = stats.Recurring;

            using var connection = JobStorage.Current.GetConnection();
            model.JobsRecorrentes = connection.GetRecurringJobs()
                .Select(r => new FilaRecorrenteItem(r.Id, r.Cron, r.LastExecution, r.NextExecution, r.LastJobState))
                .ToList();

            model.UltimasFalhas = monitor.FailedJobs(0, 15)
                .Select(j => new FilaJobItem(j.Key, FormatarJob(j.Value.Job), j.Value.ExceptionMessage ?? j.Value.Reason, j.Value.FailedAt))
                .ToList();

            model.UltimosSucessos = monitor.SucceededJobs(0, 15)
                .Select(j => new FilaJobItem(j.Key, FormatarJob(j.Value.Job), null, j.Value.SucceededAt))
                .ToList();
        }
        catch
        {
            model.HangfireDisponivel = false;
        }

        return View(model);
    }

    [HttpPost("/Fila/Reprocessar")]
    [ValidateAntiForgeryToken]
    public IActionResult Reprocessar(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            BackgroundJob.Requeue(id);
            TempData["MensagemSucesso"] = $"Job {id} reenfileirado.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string FormatarJob(Hangfire.Common.Job? job)
        => job is null ? "-" : $"{job.Type?.Name}.{job.Method?.Name}";
}
