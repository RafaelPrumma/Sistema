using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;

namespace Sistema.MVC.Controllers
{
    [AuthorizePermission("Documentacao", Permissao.Visualizar)]
    public class DocumentacaoController : Controller
    {
        [HttpGet("/Documentacao")]
        [HttpGet("/Documentacao/Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
