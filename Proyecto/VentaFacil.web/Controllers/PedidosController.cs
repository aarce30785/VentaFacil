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

        // GET: /Pedidos/Crear
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var pedido = await _pedidoService.CrearPedidoAsync(usuarioId);

                TempData["PedidoId"] = pedido.Id_Venta;
                return RedirectToAction("Editar", new { id = pedido.Id_Venta });
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
        public async Task<IActionResult> ActualizarModalidad(int pedidoId, ModalidadPedido modalidad, int? numeroMesa, string cliente)
        {
            try
            {
                var pedido = await _pedidoService.ActualizarModalidadAsync(pedidoId, modalidad, numeroMesa);

                // Actualizar el cliente
                pedido.Cliente = cliente;
                // Nota: El servicio ActualizarModalidadAsync no actualiza el cliente, así que debemos hacerlo aquí o en el servicio.
                // Como estamos en memoria, podemos actualizarlo directamente.
                // Pero es mejor tener un método en el servicio para actualizar la cabecera.

                var mensaje = modalidad == ModalidadPedido.EnMesa
                    ? $"Modalidad actualizada a 'En Mesa' (Mesa {numeroMesa})"
                    : "Modalidad actualizada a 'Para Llevar'";

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

        // POST: /Pedidos/Cancelar (NUEVA ACCIÓN PE06)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int pedidoId, string motivoCancelacion)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var usuarioId = ObtenerUsuarioId();

                if (pedido.Id_Usuario != usuarioId)
                {
                    TempData["Error"] = "No tiene permisos para cancelar este pedido";
                    return RedirectToAction("Index");
                }

                var resultado = await _pedidoService.CancelarPedidoAsync(pedidoId, motivoCancelacion);

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
                TempData["Error"] = $"Error al cancelar pedido: {ex.Message}";
                return RedirectToAction("Editar", new { id = pedidoId });
            }
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

        // NUEVA ACCIÓN PE05: Búsqueda de pedidos por ID o cliente
        // POST: /Pedidos/Buscar
        [HttpPost]
        public async Task<IActionResult> Buscar(string criterio)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();

                // 1. Obtener todas las listas necesarias para Index (mantener la estructura)
                var pedidosBorrador = await _pedidoService.ObtenerPedidosBorradorAsync(usuarioId);
                var pedidosPendientes = await _pedidoService.ObtenerPedidosPendientesAsync(usuarioId);
                var todosLosPedidos = await _pedidoService.ObtenerTodosLosPedidosAsync(usuarioId);

                ViewBag.PedidosBorrador = pedidosBorrador;
                ViewBag.PedidosPendientes = pedidosPendientes;
                ViewBag.TodosLosPedidos = todosLosPedidos;
                ViewBag.UsuarioId = usuarioId;

                // Si el criterio está vacío, simplemente mostramos el Index normal
                if (string.IsNullOrWhiteSpace(criterio))
                {
                    TempData["Error"] = "Ingrese un criterio de búsqueda (ID o Nombre de cliente).";
                    return View("Index");
                }

                // 2. Realizar la búsqueda de PE05
                var resultados = await _pedidoService.BuscarPedidosAsync(usuarioId, criterio);

                if (!resultados.Any())
                {
                    // Escenario 2: Pedido no encontrado
                    TempData["Error"] = $"Pedido no encontrado para el criterio: '{criterio}'";
                }
                else
                {
                    // Escenario 1: Asignar resultados a un ViewBag específico
                    ViewBag.PedidosBusqueda = resultados;
                    TempData["Success"] = $"Se encontraron {resultados.Count} pedidos para el criterio '{criterio}'.";
                }

                // 3. Devolver la vista Index
                return View("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al buscar pedidos: {ex.Message}";
                return RedirectToAction("Index");
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