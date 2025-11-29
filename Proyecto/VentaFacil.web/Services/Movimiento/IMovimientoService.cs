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

        /// <summary>
        /// Corrige un movimiento de inventario, actualizando el stock y generando un registro de auditoría.
        /// </summary>
        /// <param name="idMovimiento">ID del movimiento a corregir.</param>
        /// <param name="nuevaCantidad">Nueva cantidad.</param>
        /// <param name="nuevoTipo">Nuevo tipo de movimiento (Entrada/Salida).</param>
        /// <param name="motivo">Motivo de la corrección.</param>
        /// <param name="idUsuario">ID del usuario que realiza la corrección.</param>
        /// <returns>True si la corrección fue exitosa.</returns>
        Task<bool> CorregirMovimientoAsync(int idMovimiento, int nuevaCantidad, string nuevoTipo, string motivo, int idUsuario);
    }
}
