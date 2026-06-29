using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using Sistema.APP.DependencyInjection;
using Sistema.APP.Services.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.DependencyInjection;
using Sistema.MVC.DependencyInjection;
using Sistema.MVC.Infrastructure;
using Sistema.MVC.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Services.AddControllersWithViews();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSecurity();
if (builder.Environment.IsDevelopment())
{
    var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Hangfire: processa importação de arquivos em segundo plano (não trava as requisições).
var hangfireConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var hangfireEnabled = !builder.Configuration.GetValue<bool>("UseInMemoryDatabase")
    && !string.IsNullOrWhiteSpace(hangfireConnection);
if (hangfireEnabled)
{
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            QueuePollInterval = TimeSpan.FromSeconds(15),
            SchemaName = "HangFire"
        }));
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    const string correlationHeader = "X-Correlation-ID";
    var correlationId = context.Request.Headers[correlationHeader].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    context.TraceIdentifier = correlationId;
    context.Response.Headers[correlationHeader] = correlationId;
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (hangfireEnabled)
{
    app.UseHangfireDashboard("/jobs", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }

    DbInitializer.Seed(db);

    // Job recorrente de cotações (substitui o BackgroundService). Intervalo vem da Configuração;
    // como o cron é por minuto, segundos < 60 viram 1 minuto. Re-registra a cada inicialização.
    if (hangfireEnabled)
    {
        var leitura = scope.ServiceProvider.GetRequiredService<IConfiguracaoLeitura>();
        if (await leitura.ObterBoolAsync("Financas", "MarketData:BackgroundEnabled", true))
        {
            // Default de 30 min (modelo de cotações/histórico, spec F-P): o job grava o cache da última
            // cotação + o bucket intradiário 30m em FinanceiroPrecoHistoricoAtivo.
            var segundos = await leitura.ObterIntAsync("Financas", "MarketData:RefreshSeconds", 1800);
            var minutos = Math.Max(1, (int)Math.Round(segundos / 60.0));
            var cron = minutos <= 1 ? "* * * * *" : $"*/{minutos} * * * *";
            RecurringJob.AddOrUpdate<IFinancasMarketDataService>(
                "financas-cotacoes",
                s => s.AtualizarCotacoesAsync(false, CancellationToken.None),
                cron);

            // TODO (spec F-P, agenda): a coleta hoje é única (B3 + cripto no mesmo job, 24/7). A spec
            // pede B3 SÓ na janela de pregão/dias úteis e cripto 24/7. Separar exige cindir
            // AtualizarCotacoesAsync por classe (ex.: AtualizarCotacoesB3Async/CriptoAsync) + 2 crons
            // (B3 com cron de pregão, cripto a cada 30m). Coleta 30m + consolidação 1d já estão prontas.

            // Consolidação diária (1d a partir dos 30m) + retenção do intradiário. Roda após o
            // fechamento (cripto fecha 23:59 UTC = 20:59 BRT; pós-meia-noite UTC já cobre B3 e cripto).
            RecurringJob.AddOrUpdate<IFinancasMarketDataService>(
                "financas-historico-consolidacao",
                s => s.ConsolidarHistoricoDiarioAsync(CancellationToken.None),
                "10 0 * * *"); // 00:10 UTC diariamente.

            // Proventos mudam poucas vezes ao mês: busca diária na Brapi é suficiente (job idempotente).
            RecurringJob.AddOrUpdate<IFinancasMarketDataService>(
                "financas-proventos",
                s => s.AtualizarProventosAsync(false, CancellationToken.None),
                Cron.Daily());

            // Eventos corporativos (desdobramento/grupamento) raros: busca diária na Brapi, upsert idempotente.
            RecurringJob.AddOrUpdate<IFinancasMarketDataService>(
                "financas-eventos-corporativos",
                s => s.AtualizarEventosCorporativosAsync(false, CancellationToken.None),
                Cron.Daily());

            // F-B F2: benchmarks (CDI/IPCA via BCB SGS público; Ibov opcional). Mudam pouco (CDI diário,
            // IPCA mensal): busca diária + upsert idempotente. À prova de falha — fonte offline não derruba.
            RecurringJob.AddOrUpdate<IFinancasMarketDataService>(
                "financas-benchmarks",
                s => s.AtualizarBenchmarksAsync(false, CancellationToken.None),
                Cron.Daily());

            // F-H: alertas de preço/provento. Roda junto da coleta de cotações (mesmo cron) para reagir
            // logo após o preço atualizar; à prova de falha (try-catch por regra), notifica via Mensagens.
            RecurringJob.AddOrUpdate<IFinancasAlertaService>(
                "financas-alertas",
                s => s.ProcessarAlertasAsync(CancellationToken.None),
                cron);
        }
        else
        {
            RecurringJob.RemoveIfExists("financas-cotacoes");
            RecurringJob.RemoveIfExists("financas-historico-consolidacao");
            RecurringJob.RemoveIfExists("financas-proventos");
            RecurringJob.RemoveIfExists("financas-eventos-corporativos");
            RecurringJob.RemoveIfExists("financas-benchmarks");
            RecurringJob.RemoveIfExists("financas-alertas");
        }
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
