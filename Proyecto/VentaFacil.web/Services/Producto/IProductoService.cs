
﻿using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public interface IProductoService
    {
        Task<ListProductoResponse> ListarTodosAsync();
        Task<RegisterProductoResponse> RegisterAsync(ProductoDto productoDto);
        Task<EditProductoResponse> EditarAsync(ProductoDto productoDto);
        Task<ProductoDto?> ObtenerPorIdAsync(int idProducto);
        Task<EditProductoResponse> EliminarAsync(int idProducto);
        Task<EditProductoResponse> DeshabilitarAsync(int idProducto);
        Task<EditProductoResponse> HabilitarAsync(int idProducto);
    }
}
