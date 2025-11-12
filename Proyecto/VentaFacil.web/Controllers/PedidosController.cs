using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IPedidoService _pedidoService;
        private readonly IProductoService _productoService;

        public PedidosController(IPedidoService pedidoService, IProductoService productoService)
        {
            _pedidoService = pedidoService;
            _productoService = productoService;
        }

        // GET: /Pedidos/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var pedidosBorrador = await _pedidoService.ObtenerPedidosBorradorAsync(usuarioId);
                var pedidosPendientes = await _pedidoService.ObtenerPedidosPendientesAsync(usuarioId);
                var todosLosPedidos = await _pedidoService.ObtenerTodosLosPedidosAsync(usuarioId);

                ViewBag.PedidosBorrador = pedidosBorrador;
                ViewBag.PedidosPendientes = pedidosPendientes;
                ViewBag.TodosLosPedidos = todosLosPedidos;
                ViewBag.UsuarioId = usuarioId; // Para debugging

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar pedidos: {ex.Message}";
                return View();
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

                
                var productosResponse = await _productoService.ListarTodosAsync();
                ViewBag.Productos = productosResponse.Success ? productosResponse.Productos : new List<ProductoDto>();

                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear pedido: {ex.Message}";
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
                if (pedido.Id_Usuario != usuarioId)
                {
                    TempData["Error"] = "No tiene permisos para editar este pedido";
                    return RedirectToAction("Index");
                }

               
                var productosResponse = await _productoService.ListarTodosAsync();
                ViewBag.Productos = productosResponse.Success ? productosResponse.Productos : new List<ProductoDto>();

                return View(pedido);
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Pedido no encontrado";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar pedido: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cocina()
        {
            try
            {
                var pedidosCocina = await _pedidoService.ObtenerPedidosParaCocinaAsync();
                return View(pedidosCocina);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar pedidos de cocina: {ex.Message}";
                return View(new List<PedidoDto>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNuevoPedido(int pedidoId, string cliente, ModalidadPedido modalidad,
            int? numeroMesa, string tipoGuardado, List<ItemPedido> productos)
        {
            try
            {
                // Actualizar información básica del pedido
                var pedido = await _pedidoService.ActualizarClienteAsync(pedidoId, cliente);
                pedido = await _pedidoService.ActualizarModalidadAsync(pedidoId, modalidad, numeroMesa);

                // Agregar productos al pedido
                if (productos != null)
                {
                    foreach (var item in productos)
                    {
                        await _pedidoService.AgregarProductoAsync(pedidoId, item.ProductoId, item.Cantidad);
                    }
                }

                // Guardar según el tipo seleccionado
                if (tipoGuardado == "completo")
                {
                    var resultado = await _pedidoService.GuardarPedidoAsync(pedidoId);
                    if (resultado.Success)
                    {
                        TempData["Success"] = resultado.Message;
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = resultado.Message;
                        return RedirectToAction("Crear");
                    }
                }
                else // borrador
                {
                    var resultado = await _pedidoService.GuardarComoBorradorAsync(pedidoId);
                    if (resultado.Success)
                    {
                        TempData["Success"] = resultado.Message;
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = resultado.Message;
                        return RedirectToAction("Crear");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar pedido: {ex.Message}";
                return RedirectToAction("Crear");
            }
        }

        public class ItemPedido
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }

        // POST: /Pedidos/AgregarProducto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarProducto(int pedidoId, int productoId, int cantidad = 1)
        {
            try
            {
                var pedido = await _pedidoService.AgregarProductoAsync(pedidoId, productoId, cantidad);
                TempData["Success"] = "Producto agregado correctamente";

                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al agregar producto: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // POST: /Pedidos/ActualizarCantidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(int pedidoId, int itemId, int cantidad)
        {
            try
            {
                var pedido = await _pedidoService.ActualizarCantidadProductoAsync(pedidoId, itemId, cantidad);

                if (cantidad <= 0)
                    TempData["Success"] = "Producto eliminado del pedido";
                else
                    TempData["Success"] = "Cantidad actualizada correctamente";

                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar cantidad: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // POST: /Pedidos/EliminarProducto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProducto(int pedidoId, int itemId)
        {
            try
            {
                var pedido = await _pedidoService.EliminarProductoAsync(pedidoId, itemId);
                TempData["Success"] = "Producto eliminado correctamente";

                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar producto: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // POST: /Pedidos/ActualizarModalidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarModalidad(int pedidoId, ModalidadPedido modalidad, int? numeroMesa)
        {
            try
            {
                var pedido = await _pedidoService.ActualizarModalidadAsync(pedidoId, modalidad, numeroMesa);

                string mensaje;
                if (modalidad == ModalidadPedido.EnMesa)
                {
                    if (numeroMesa.HasValue && numeroMesa > 0)
                    {
                        mensaje = $"Modalidad actualizada a 'En Mesa' (Mesa {numeroMesa})";
                    }
                    else
                    {
                        mensaje = "Modalidad actualizada a 'En Mesa'. Recuerde asignar un número de mesa.";
                    }
                }
                else
                {
                    mensaje = "Modalidad actualizada a 'Para Llevar'";
                }

                TempData["Success"] = mensaje;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar modalidad: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // POST: /Pedidos/GuardarPedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPedido(int pedidoId)
        {
            try
            {
                
                var esValido = await _pedidoService.ValidarPedidoParaGuardarAsync(pedidoId);

                if (!esValido)
                {
                    TempData["Error"] = "No se puede guardar el pedido. Verifique que: \n" +
                                       "- Tenga al menos un producto \n" +
                                       "- Tenga un cliente asignado \n" +
                                       "- Si es en mesa, tenga un número de mesa válido";
                    return RedirectToAction("Editar", new { id = pedidoId });
                }

                var resultado = await _pedidoService.GuardarPedidoAsync(pedidoId);

                if (resultado.Success)
                {
                    TempData["Success"] = resultado.Message;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = resultado.Message;
                    return RedirectToAction("Editar", new { id = pedidoId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar pedido: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        // POST: /Pedidos/GuardarBorrador
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
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = resultado.Message;
                    return RedirectToAction("Editar", new { id = pedidoId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar borrador: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        

        // GET: /Pedidos/ContinuarBorrador/{id}
        [HttpGet]
        public async Task<IActionResult> ContinuarBorrador(int id)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPedidoAsync(id);
                var usuarioId = ObtenerUsuarioId();

                if (pedido.Id_Usuario != usuarioId)
                {
                    TempData["Error"] = "No tiene permisos para editar este pedido";
                    return RedirectToAction("Index");
                }

                if (!await _pedidoService.PuedeEditarseAsync(id))
                {
                    TempData["Error"] = "Este pedido no puede ser editado";
                    return RedirectToAction("Index");
                }

                return RedirectToAction("Editar", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al continuar borrador: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCliente(int pedidoId, string cliente)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cliente))
                {
                    TempData["Error"] = "El nombre del cliente es requerido";
                    return RedirectToAction("Editar", new { id = pedidoId });
                }

                var pedido = await _pedidoService.ActualizarClienteAsync(pedidoId, cliente);
                TempData["Success"] = "Cliente actualizado correctamente";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Editar", new { id = pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar cliente: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoListo([FromBody] MarcarListoRequest request)
        {
            try
            {
                Console.WriteLine($"=== MARCAR COMO LISTO INICIADO ===");
                Console.WriteLine($"Pedido ID recibido: {request.PedidoId}");

                var resultado = await _pedidoService.MarcarComoListoAsync(request.PedidoId);

                Console.WriteLine($"Resultado - Success: {resultado.Success}, Message: {resultado.Message}");

                if (resultado.Success)
                {
                    Console.WriteLine("=== MARCAR COMO LISTO EXITOSO ===");
                    return Json(new { success = true, message = resultado.Message });
                }
                else
                {
                    Console.WriteLine("=== MARCAR COMO LISTO FALLIDO ===");
                    return Json(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN MARCAR COMO LISTO ===");
                Console.WriteLine($"Excepción: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class MarcarListoRequest
        {
            public int PedidoId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarNotaCocina([FromBody] NotaCocinaRequest request)
        {
            try
            {
                Console.WriteLine($"=== AGREGAR NOTA COCINA ===");
                Console.WriteLine($"Pedido ID: {request.Id}, Nota: {request.Nota}");

                var resultado = await _pedidoService.AgregarNotaCocinaAsync(request.Id, request.Nota);

                if (resultado.Success)
                {
                    return Json(new { success = true, message = resultado.Message });
                }
                else
                {
                    return Json(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        public class NotaCocinaRequest
        {
            public int Id { get; set; }
            public string Nota { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarPedido(int id, string razon)
        {
            try
            {
                var resultado = await _pedidoService.CancelarPedidoAsync(id, razon);

                if (resultado.Success)
                {
                    TempData["Success"] = resultado.Message;
                }
                else
                {
                    TempData["Error"] = resultado.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cancelar pedido: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string termino)
        {
            try
            {
                var resultado = await _pedidoService.BuscarPedidosAsync(termino);
                return Json(new { success = true, pedidos = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarPreparacion(int id)
        {
            try
            {
                Console.WriteLine($"=== INICIAR PREPARACION INICIADO ===");
                Console.WriteLine($"Pedido ID: {id}");

                var resultado = await _pedidoService.IniciarPreparacionAsync(id);

                Console.WriteLine($"Resultado - Success: {resultado.Success}, Message: {resultado.Message}");

                if (resultado.Success)
                {
                    Console.WriteLine("=== INICIAR PREPARACION EXITOSO ===");
                    return Json(new { success = true, message = resultado.Message });
                }
                else
                {
                    Console.WriteLine("=== INICIAR PREPARACION FALLIDO ===");
                    return Json(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN INICIAR PREPARACION ===");
                Console.WriteLine($"Excepción: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoEntregado([FromBody] MarcarListoRequest request)
        {
            try
            {
                var resultado = await _pedidoService.MarcarComoEntregadoAsync(request.PedidoId);

                if (resultado.Success)
                {
                    return Json(new { success = true, message = "Pedido marcado como entregado" });
                }
                else
                {
                    return Json(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPedidosCocina()
        {
            try
            {
                var pedidos = await _pedidoService.ObtenerPedidosParaCocinaAsync();
                
                return Json(new { actualizado = true, count = pedidos.Count });
            }
            catch (Exception)
            {
                return Json(new { actualizado = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEstado(int id)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPedidoAsync(id);
                return Json(new
                {
                    success = true,
                    estado = pedido.Estado.ToString(),
                    puedeIniciarPreparacion = pedido.Estado == PedidoEstado.Borrador || pedido.Estado == PedidoEstado.Pendiente,
                    puedeMarcarListo = pedido.Estado == PedidoEstado.EnPreparacion,
                    puedeMarcarEntregado = pedido.Estado == PedidoEstado.Listo
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerResumenPedidos()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var pedidosBorrador = await _pedidoService.ObtenerPedidosBorradorAsync(usuarioId);
                var pedidosPendientes = await _pedidoService.ObtenerPedidosPendientesAsync(usuarioId);
                var todosLosPedidos = await _pedidoService.ObtenerTodosLosPedidosAsync(usuarioId);

                // Verificar si hay cambios significativos (pedidos que cambiaron de estado)
                var cambiosSignificativos = todosLosPedidos.Any(p =>
                    p.Estado == PedidoEstado.Listo ||
                    p.Estado == PedidoEstado.Entregado);

                return Json(new
                {
                    actualizado = true,
                    cambiosSignificativos = cambiosSignificativos,
                    estadisticas = new
                    {
                        borrador = pedidosBorrador.Count,
                        enCocina = pedidosPendientes.Count,
                        listos = todosLosPedidos.Count(p => p.Estado == PedidoEstado.Listo),
                        entregados = todosLosPedidos.Count(p => p.Estado == PedidoEstado.Entregado),
                        cancelados = todosLosPedidos.Count(p => p.Estado == PedidoEstado.Cancelado)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { actualizado = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarPreparacion([FromBody] MarcarListoRequest request)
        {
            try
            {
                Console.WriteLine($"=== INICIAR PREPARACION INICIADO ===");
                Console.WriteLine($"Pedido ID: {request.PedidoId}");

                
                var resultado = await _pedidoService.IniciarPreparacionAsync(request.PedidoId);

                Console.WriteLine($"Resultado - Success: {resultado.Success}, Message: {resultado.Message}");

                if (resultado.Success)
                {
                    Console.WriteLine("=== INICIAR PREPARACION EXITOSO ===");
                    return Json(new { success = true, message = resultado.Message });
                }
                else
                {
                    Console.WriteLine("=== INICIAR PREPARACION FALLIDO ===");
                    return Json(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN INICIAR PREPARACION ===");
                Console.WriteLine($"Excepción: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        private int ObtenerUsuarioId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                // Buscar el claim que contiene el ID del usuario
                var idClaim = User.FindFirst("UsuarioId") ??
                             User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ??
                             User.FindFirst("sub"); // Para JWT tokens

                if (idClaim != null && int.TryParse(idClaim.Value, out var usuarioId))
                    return usuarioId;

                // Si no se puede parsear, buscar por nombre de usuario (solo para desarrollo)
                var nameClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name);
                if (nameClaim != null)
                {
                    // Esto es solo para desarrollo - en producción deberías tener el ID real
                    return Math.Abs(nameClaim.Value.GetHashCode()) % 1000 + 1;
                }
            }

            // Para desarrollo, retornar un ID por defecto
            // En producción, esto debería redirigir al login
            return 1;
        }
    }
}
