using Microsoft.EntityFrameworkCore;
using Sistema.APP.DependencyInjection;
using Sistema.INFRA.Data;
using Sistema.INFRA.DependencyInjection;
using Sistema.MVC.DependencyInjection;
using Sistema.MVC.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSecurity();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }

    DbInitializer.Seed(db);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
