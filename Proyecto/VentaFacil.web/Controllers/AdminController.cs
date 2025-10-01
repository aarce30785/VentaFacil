using Microsoft.AspNetCore.Mvc;

namespace VentaFacil.web.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
