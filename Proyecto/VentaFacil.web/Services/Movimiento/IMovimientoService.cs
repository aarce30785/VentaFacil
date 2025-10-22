using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Movimiento
{
    public interface IMovimientoService
    {
        /// <summary>
        /// Obtiene el historial de movimientos de inventario, filtrando opcionalmente por inventario y rango de fechas.
        /// </summary>
        /// <param name="idInventario">ID del inventario a consultar (opcional).</param>
        /// <param name="fechaInicio">Fecha inicial del filtro (opcional).</param>
        /// <param name="fechaFin">Fecha final del filtro (opcional).</param>
        /// <returns>Lista de movimientos de inventario.</returns>
        Task<List<InventarioMovimientoDto>> ListarMovimientosAsync(int? idInventario, DateTime? fechaInicio, DateTime? fechaFin);
    }
}
