using Microsoft.AspNetCore.Mvc;
using Sistema.CORE.Enums;
using Sistema.MVC.Authorization;

namespace Sistema.MVC.Controllers
{
    [AuthorizePermission("Documentacao", Permissao.Visualizar)]
    public class DocumentacaoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
