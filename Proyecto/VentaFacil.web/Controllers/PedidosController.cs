using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Pedido;

namespace VentaFacil.web.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IRegisterPedidoService _registerPedidoService;

        public PedidosController(IRegisterPedidoService registerPedidoService)
        {
            _registerPedidoService = registerPedidoService;
        }

        // GET: /Pedidos/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new PedidoDto();
            return View(model);
        }

        // POST: /Pedidos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PedidoDto pedido)
        {
            if (!ModelState.IsValid)
                return View(pedido);


            // Asegurar que el pedido tenga asignado el usuario logueado
            if (pedido.Id_Usuario == 0 && User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst("UsuarioId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var uid))
                    pedido.Id_Usuario = uid;
            }
            var response = await _registerPedidoService.RegisterAsync(pedido);

            if (response.Success)
            {
                TempData["Mensaje"] = response.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, response.Message);
            return View(pedido);
        }

        // GET: /Pedidos/Index
        [HttpGet]
        public IActionResult Index()
        {
            // Por ahora solo mostramos la vista vacía como evidencia de avance
            return View();
        }
    }
}
