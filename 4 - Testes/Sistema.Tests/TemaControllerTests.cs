using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Sistema.APP.Services.Interfaces;
using Sistema.MVC.Controllers;
using Sistema.MVC.Models;

namespace Sistema.Tests;

public class TemaControllerTests
{
    [Fact]
    public async Task EditGetDeveRedirecionarParaLoginQuandoUsuarioNaoEstiverAutenticado()
    {
        var temaService = new Mock<ITemaAppService>();
        var logger = new Mock<ILogger<TemaController>>();
        var controller = new TemaController(temaService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Session = new TestSession()
                }
            }
        };

        var resultado = await controller.Edit();

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
    }

    [Fact]
    public async Task EditPostDeveRetornarBadRequestQuandoModelStateInvalidoEmRequisicaoAjax()
    {
        var temaService = new Mock<ITemaAppService>();
        var logger = new Mock<ILogger<TemaController>>();
        var httpContext = new DefaultHttpContext
        {
            Session = new TestSession()
        };
        httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

        var controller = new TemaController(temaService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
        controller.ModelState.AddModelError("CorHeader", "Campo obrigatório");

        var resultado = await controller.Edit(new TemaViewModel());

        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}
