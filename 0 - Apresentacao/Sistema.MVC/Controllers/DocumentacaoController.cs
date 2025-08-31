using Microsoft.AspNetCore.Mvc;

namespace Sistema.MVC.Controllers
{
    public class DocumentacaoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
