using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sistema.MVC.Services;

namespace Sistema.Tests;

public class HttpExecutionContextTests
{
    [Fact]
    public void UsuarioDevePriorizarClaimNameIdentifier()
    {
        var ctx = new DefaultHttpContext();
        ctx.TraceIdentifier = "corr-1";
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        }, "mock"));

        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var sut = new HttpExecutionContext(accessor);

        Assert.Equal("123", sut.Usuario);
        Assert.Equal("corr-1", sut.CorrelationId);
    }
}
