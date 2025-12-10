using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Services.Caja;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using System.Threading.Tasks;
using System.Linq;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly ICajaService _cajaService;

        public CajaController(ICajaService cajaService)
        {
            _cajaService = cajaService;
        }

        // Acción para listar todas las cajas
        public async Task<IActionResult> Listar()
        {
            var cajas = await _cajaService.ListarCajasAsync();
            var cajasDto = cajas.Select(c => new CajaDto
            {
                Id_Caja = c.Id_Caja,
                Id_Usuario = c.Id_Usuario,
                Fecha_Apertura = c.Fecha_Apertura,
                Fecha_Cierre = c.Fecha_Cierre,
                Monto_Inicial = c.Monto_Inicial,
                Monto = c.Monto,
                Estado = c.Estado
                
            }).ToList();

            return View(cajasDto); 
        }

        // GET: CajaController/Abrir
        public IActionResult Abrir()
        {
            return View();
        }

        // POST: CajaController/Abrir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(decimal montoInicial)
        {
            if (ModelState.IsValid)
            {

                int idUsuario = 1; //Obtener el ID del usuario autenticado
                await _cajaService.AbrirCajaAsync(idUsuario, montoInicial);
                return RedirectToAction(nameof(Listar));
            }
            
            return RedirectToAction(nameof(Listar));
        }

        // GET: CajaController/Cerrar/5
        public IActionResult Cerrar(int id)
        {
            ViewBag.IdCaja = id;
            return View();
        }

        // POST: CajaController/Cerrar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarConfirmado(int id)
        {
            if (ModelState.IsValid)
            {
                
                await _cajaService.CerrarCajaAsync(id);
                return RedirectToAction(nameof(Listar));
            }
            ViewBag.IdCaja = id;
            return View();
        }

        // GET: CajaController/Retiro/5
        public IActionResult Retiro(int id)
        {
            ViewBag.IdCaja = id;
            return View();
        }

        // POST: CajaController/Retiro/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retiro(int id, decimal monto, string motivo)
        {
            if (ModelState.IsValid)
            {
                
                int idUsuario = 1; //Obtener el ID del usuario autenticado

                await _cajaService.RegistrarRetiroAsync(id, idUsuario, monto, motivo);
                return RedirectToAction(nameof(Listar));
            }
            ViewBag.IdCaja = id;
            return View();
        }

        // GET: CajaController/Retiros/5
        public async Task<IActionResult> Retiros(int id)
        {
            var retiros = await _cajaService.ObtenerRetirosPorCajaAsync(id);
            return PartialView("_RetirosCajaPartial", retiros);
        }
    }
}
