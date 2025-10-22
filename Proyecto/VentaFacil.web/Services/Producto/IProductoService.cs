
﻿using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public interface IProductoService
    {
        Task<ListProductoResponse> ListarTodosAsync();
        Task<RegisterProductoResponse> RegisterAsync(ProductoDto productoDto);
        Task<EditProductoResponse> EditarAsync(ProductoDto productoDto);
    }
}
