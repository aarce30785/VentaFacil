using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IPedidoService _pedidoService;
        private readonly IProductoService _productoService;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(IPedidoService pedidoService, IProductoService productoService, ILogger<PedidosController> logger)
        {
            _pedidoService = pedidoService;
            _productoService = productoService;
            _logger = logger;
        }

        // GET: /Pedidos/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var modelo = await CargarModeloVistaIndex(usuarioId);
                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar pedidos para usuario");
                TempData["Error"] = "Error al cargar los pedidos";
                return View(new PedidosIndexViewModel());
            }
        }

        // GET: /Pedidos/Cocina
        [HttpGet]
        public async Task<IActionResult> Cocina()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var pedidos = await _pedidoService.ObtenerPedidosParaCocinaAsync();
                return View(pedidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de cocina");
                TempData["Error"] = "Error al cargar el panel de cocina";
                return View(new List<PedidoDto>());
            }
        }

        // GET: /Pedidos/Crear
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var pedido = await _pedidoService.CrearPedidoAsync(usuarioId);
                await CargarProductosEnViewBag();

                TempData["Success"] = "Nuevo pedido creado. Agregue productos y complete la información.";
                return View("Editar", pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pedido");
                TempData["Error"] = "Error al crear nuevo pedido";
                return RedirectToAction("Index");
            }
        }

        // GET: /Pedidos/Editar/{id}
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPedidoAsync(id);
                var usuarioId = ObtenerUsuarioId();

                if (!await ValidarPermisosEdicion(pedido, usuarioId))
                    return RedirectToAction("Index");

                await CargarProductosEnViewBag();
                return View(pedido);
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar pedido {PedidoId} para edición", id);
                TempData["Error"] = "Error al cargar el pedido";
                return RedirectToAction("Index");
            }
        }

        // POST: /Pedidos/ProcederAlPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcederAlPago(int pedidoId)
        {
            try
            {
                var resultadoValidacion = await ValidarPedidoParaPago(pedidoId);
                if (!resultadoValidacion.EsValido)
                {
                    TempData["Error"] = resultadoValidacion.Mensaje;
                    return RedirectToAction("Editar", new { id = pedidoId });
                }

                // Si está en borrador, guardarlo antes de proceder al pago
                if (resultadoValidacion.Pedido.Estado == PedidoEstado.Borrador)
                {
                    var resultadoGuardado = await _pedidoService.GuardarPedidoAsync(pedidoId);
                    if (!resultadoGuardado.Success)
                    {
                        TempData["Error"] = resultadoGuardado.Message;
                        return RedirectToAction("Editar", new { id = pedidoId });
                    }
                }

                return RedirectToAction("ProcesarPago", "Facturacion", new { pedidoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al proceder al pago para pedido {PedidoId}", pedidoId);
                TempData["Error"] = "Error al procesar el pago";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // MÉTODOS DE GESTIÓN DE PRODUCTOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarProducto(int pedidoId, int productoId, int cantidad = 1)
        {
            return await EjecutarOperacionPedidoAsync(
                () => _pedidoService.AgregarProductoAsync(pedidoId, productoId, cantidad),
                pedidoId,
                "Producto agregado correctamente"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(int pedidoId, int itemId, int cantidad)
        {
            var mensaje = cantidad <= 0 ? "Producto eliminado del pedido" : "Cantidad actualizada correctamente";
            return await EjecutarOperacionPedidoAsync(
                () => _pedidoService.ActualizarCantidadProductoAsync(pedidoId, itemId, cantidad),
                pedidoId,
                mensaje
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProducto(int pedidoId, int itemId)
        {
            return await EjecutarOperacionPedidoAsync(
                () => _pedidoService.EliminarProductoAsync(pedidoId, itemId),
                pedidoId,
                "Producto eliminado correctamente"
            );
        }

        // MÉTODOS DE ACTUALIZACIÓN DE DATOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarModalidad(int pedidoId, ModalidadPedido modalidad, int? numeroMesa)
        {
            return await EjecutarOperacionPedidoAsync(
                () => _pedidoService.ActualizarModalidadAsync(pedidoId, modalidad, numeroMesa),
                pedidoId,
                $"Modalidad actualizada a '{modalidad}'"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCliente(int pedidoId, string cliente)
        {
            return await EjecutarOperacionPedidoAsync(
                () => _pedidoService.ActualizarClienteAsync(pedidoId, cliente),
                pedidoId,
                "Cliente actualizado correctamente"
            );
        }

        // MÉTODOS DE GESTIÓN DE ESTADO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarBorrador(int pedidoId)
        {
            try
            {
                var resultado = await _pedidoService.GuardarComoBorradorAsync(pedidoId);
                if (resultado.Success)
                {
                    TempData["Success"] = resultado.Message;
                }
                else
                {
                    TempData["Error"] = resultado.Message;
                }
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar borrador {PedidoId}", pedidoId);
                TempData["Error"] = "Error al guardar como borrador";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // MÉTODOS API PARA COCINA
        [HttpPost]
        public async Task<IActionResult> IniciarPreparacion([FromBody] AccionPedidoRequest request)
        {
            return await EjecutarAccionEstadoAsync(
                async () => 
                {
                    // Validaciones antes de enviar a cocina
                    var pedido = await _pedidoService.ObtenerPedidoAsync(request.PedidoId);
                    
                    if (!pedido.Items.Any())
                        return new ServiceResult { Success = false, Message = "El pedido debe tener al menos un producto." };

                    if (string.IsNullOrWhiteSpace(pedido.Cliente))
                        return new ServiceResult { Success = false, Message = "El nombre del cliente es obligatorio." };

                    if (pedido.Modalidad == ModalidadPedido.EnMesa && (!pedido.NumeroMesa.HasValue || pedido.NumeroMesa <= 0))
                        return new ServiceResult { Success = false, Message = "Debe especificar un número de mesa válido." };

                    return await _pedidoService.IniciarPreparacionAsync(request.PedidoId);
                }
            );
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoListo([FromBody] AccionPedidoRequest request)
        {
            return await EjecutarAccionEstadoAsync(
                () => _pedidoService.MarcarComoListoAsync(request.PedidoId)
            );
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoEntregado([FromBody] AccionPedidoRequest request)
        {
            return await EjecutarAccionEstadoAsync(
                () => _pedidoService.MarcarComoEntregadoAsync(request.PedidoId)
            );
        }

        [HttpPost]
        public async Task<IActionResult> CancelarPedido([FromBody] CancelarPedidoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Razon))
                    return BadRequest(new { success = false, message = "La razón de cancelación es requerida" });

                var resultado = await _pedidoService.CancelarPedidoAsync(request.PedidoId, request.Razon);
                return Json(new { success = resultado.Success, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar pedido {PedidoId}", request?.PedidoId);
                return Json(new { success = false, message = "Error al cancelar el pedido" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AgregarNotaCocina([FromBody] NotaPedidoRequest request)
        {
            try
            {
                var resultado = await _pedidoService.AgregarNotaCocinaAsync(request.PedidoId, request.Nota);
                return Json(new { success = resultado.Success, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar nota al pedido {PedidoId}", request?.PedidoId);
                return Json(new { success = false, message = "Error al agregar nota" });
            }
        }

        // MÉTODOS DE CONSULTA API
        [HttpGet]
        public async Task<IActionResult> ObtenerResumenPedidos()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var resumen = await _pedidoService.ObtenerResumenPedidosAsync(usuarioId);

                return Json(new
                {
                    actualizado = true,
                    estadisticas = resumen,
                    timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de pedidos");
                return Json(new { actualizado = false, error = "Error al obtener resumen" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPedidosCocina()
        {
            try
            {
                var pedidos = await _pedidoService.ObtenerPedidosParaCocinaAsync();
                return Json(new
                {
                    actualizado = true,
                    count = pedidos.Count,
                    pedidos = pedidos.Select(p => new {
                        id = p.Id_Venta,
                        estado = p.Estado.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pedidos para cocina");
                return Json(new { actualizado = false });
            }
        }

        // MÉTODOS AUXILIARES PRIVADOS
        private async Task<PedidosIndexViewModel> CargarModeloVistaIndex(int usuarioId)
        {
            var pedidos = await _pedidoService.ObtenerTodosLosPedidosAsync(usuarioId);

            return new PedidosIndexViewModel
            {
                PedidosBorrador = pedidos.Where(p => p.Estado == PedidoEstado.Borrador).ToList(),
                PedidosPendientes = pedidos.Where(p => p.Estado == PedidoEstado.Pendiente || p.Estado == PedidoEstado.EnPreparacion).ToList(),
                PedidosListos = pedidos.Where(p => p.Estado == PedidoEstado.Listo).ToList(),
                PedidosEntregados = pedidos.Where(p => p.Estado == PedidoEstado.Entregado).ToList(),
                PedidosCancelados = pedidos.Where(p => p.Estado == PedidoEstado.Cancelado).ToList(),
                TodosLosPedidos = pedidos,
                UsuarioId = usuarioId
            };
        }

        private async Task CargarProductosEnViewBag()
        {
            var productosResponse = await _productoService.ListarTodosAsync();
            ViewBag.Productos = productosResponse.Success ? productosResponse.Productos : new List<ProductoDto>();
        }

        private async Task<bool> ValidarPermisosEdicion(PedidoDto pedido, int usuarioId)
        {
            if (pedido.Id_Usuario != usuarioId)
            {
                TempData["Error"] = "No tiene permisos para editar este pedido";
                return false;
            }

            if (!await _pedidoService.PuedeEditarseAsync(pedido.Id_Venta))
            {
                TempData["Error"] = "Este pedido no puede ser editado en su estado actual";
                return false;
            }

            return true;
        }

        private async Task<ValidacionPagoResult> ValidarPedidoParaPago(int pedidoId)
        {
            var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);

            if (pedido.Estado != PedidoEstado.Borrador && pedido.Estado != PedidoEstado.Pendiente)
                return new ValidacionPagoResult { EsValido = false, Mensaje = $"El pedido no está listo para procesar pago. Estado actual: {pedido.Estado}" };

            var esValido = await _pedidoService.ValidarPedidoParaGuardarAsync(pedidoId);
            if (!esValido)
                return new ValidacionPagoResult { EsValido = false, Mensaje = "No se puede proceder al pago. Verifique los datos del pedido." };

            return new ValidacionPagoResult { EsValido = true, Pedido = pedido };
        }

        private async Task<IActionResult> EjecutarOperacionPedidoAsync(Func<Task<PedidoDto>> operacion, int pedidoId, string mensajeExito)
        {
            try
            {
                await operacion();
                TempData["Success"] = mensajeExito;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en operación de pedido {PedidoId}", pedidoId);
                TempData["Error"] = "Error en la operación";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        private async Task<IActionResult> EjecutarAccionEstadoAsync(Func<Task<ServiceResult>> operacion)
        {
            try
            {
                var resultado = await operacion();
                return Json(new { success = resultado.Success, message = resultado.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en acción de estado");
                return Json(new { success = false, message = "Error en la operación" });
            }
        }

        private int ObtenerUsuarioId()
        {
            if (User?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("Usuario no autenticado");

            var idClaim = User.FindFirst("UsuarioId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out var usuarioId))
                return usuarioId;

            throw new UnauthorizedAccessException("Usuario no válido");
        }

        // CLASES INTERNAS
        public class AccionPedidoRequest
        {
            public int PedidoId { get; set; }
        }

        public class CancelarPedidoRequest
        {
            public int PedidoId { get; set; }
            public string Razon { get; set; }
        }

        public class NotaPedidoRequest
        {
            public int PedidoId { get; set; }
            public string Nota { get; set; }
        }

        private class ValidacionPagoResult
        {
            public bool EsValido { get; set; }
            public string Mensaje { get; set; }
            public PedidoDto Pedido { get; set; }
        }
    }

    // ViewModel para la vista Index
    public class PedidosIndexViewModel
    {
        public List<PedidoDto> PedidosBorrador { get; set; } = new();
        public List<PedidoDto> PedidosPendientes { get; set; } = new();
        public List<PedidoDto> PedidosListos { get; set; } = new();
        public List<PedidoDto> PedidosEntregados { get; set; } = new();
        public List<PedidoDto> PedidosCancelados { get; set; } = new();
        public List<PedidoDto> TodosLosPedidos { get; set; } = new();
        public int UsuarioId { get; set; }
    }
}