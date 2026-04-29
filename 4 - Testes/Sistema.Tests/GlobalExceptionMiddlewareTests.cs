using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Sistema.MVC.Middleware;

namespace Sistema.Tests;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncDeveRetornar422ParaArgumentException()
    {
        var ctx = new DefaultHttpContext();
        ctx.TraceIdentifier = "trace-422";
        var middleware = new GlobalExceptionMiddleware(_ => throw new ArgumentException("inválido"), NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        Assert.Equal(422, ctx.Response.StatusCode);
        ctx.Response.Body.Position = 0;
    }

    [Fact]
    public async Task InvokeAsyncDeveRetornar500ParaErroInesperado()
    {
        var ctx = new DefaultHttpContext();
        ctx.TraceIdentifier = "trace-500";
        using var stream = new MemoryStream();
        ctx.Response.Body = stream;

        var middleware = new GlobalExceptionMiddleware(_ => throw new InvalidOperationException("boom"), NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        Assert.Equal(500, ctx.Response.StatusCode);
        stream.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
        Assert.True(payload.GetProperty("Success").ValueKind is JsonValueKind.True or JsonValueKind.False);
    }
}
