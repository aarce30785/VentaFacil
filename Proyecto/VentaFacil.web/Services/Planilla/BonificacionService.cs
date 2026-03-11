using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services.Planilla
{
    public class BonificacionService : IBonificacionService
    {
        private readonly ApplicationDbContext _context;

        public BonificacionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BaseResponse> AgregarBonificacionAsync(BonificacionDto dto, int idUsuarioResponsable)
        {
            var response = new BaseResponse();
            try
            {
                var planilla = await _context.Planilla.FindAsync(dto.Id_Planilla);
                if (planilla == null)
                {
                    response.Success = false;
                    response.Message = "Planilla no encontrada.";
                    return response;
                }

                var bonificacion = new Bonificacion
                {
                    Id_Planilla = dto.Id_Planilla,
                    Monto = dto.Monto,
                    Motivo = dto.Motivo,
                    Fecha = dto.Fecha,
                    FechaRegistro = DateTime.Now
                };

                _context.Bonificacion.Add(bonificacion);
                
                // Actualizar total bonificaciones en planilla
                planilla.Bonificaciones += dto.Monto;
                // Recalcular Bruto
                planilla.SalarioBruto += dto.Monto;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Bonificación agregada correctamente.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al agregar bonificación: " + ex.Message;
            }
            return response;
        }

        public async Task<BaseResponse> EditarBonificacionAsync(int idBonificacion, BonificacionDto dto, int idUsuarioResponsable)
        {
            var response = new BaseResponse();
            try
            {
                var bonificacion = await _context.Bonificacion.FindAsync(idBonificacion);
                if (bonificacion == null)
                {
                    response.Success = false;
                    response.Message = "Bonificación no encontrada.";
                    return response;
                }

                var planilla = await _context.Planilla.FindAsync(bonificacion.Id_Planilla);
                if (planilla == null)
                {
                    response.Success = false;
                    response.Message = "Planilla asociada no encontrada.";
                    return response;
                }

                // Guardar historial
                var auditoria = new BonificacionAuditoria
                {
                    Id_Bonificacion = bonificacion.Id,
                    MontoAnterior = bonificacion.Monto,
                    MontoNuevo = dto.Monto,
                    MotivoCambio = "Edición de bonificación",
                    Id_UsuarioResponsable = idUsuarioResponsable,
                    FechaCambio = DateTime.Now
                };
                _context.BonificacionAuditoria.Add(auditoria);

                // Revertir monto anterior de la planilla
                planilla.Bonificaciones -= bonificacion.Monto;
                planilla.SalarioBruto -= bonificacion.Monto;

                // Actualizar bonificación
                bonificacion.Monto = dto.Monto;
                bonificacion.Motivo = dto.Motivo;
                bonificacion.Fecha = dto.Fecha;

                // Aplicar nuevo monto a la planilla
                planilla.Bonificaciones += dto.Monto;
                planilla.SalarioBruto += dto.Monto;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Bonificación actualizada correctamente.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al editar bonificación: " + ex.Message;
            }
            return response;
        }

        public async Task<BaseResponse> EliminarBonificacionAsync(int idBonificacion, int idUsuarioResponsable)
        {
            var response = new BaseResponse();
            try
            {
                var bonificacion = await _context.Bonificacion.FindAsync(idBonificacion);
                if (bonificacion == null)
                {
                    response.Success = false;
                    response.Message = "Bonificación no encontrada.";
                    return response;
                }

                var planilla = await _context.Planilla.FindAsync(bonificacion.Id_Planilla);
                if (planilla != null)
                {
                    // Revertir montos
                    planilla.Bonificaciones -= bonificacion.Monto;
                    planilla.SalarioBruto -= bonificacion.Monto;
                }

                // Auditoría de eliminación (opcional, o soft delete)
                // Por ahora hard delete
                _context.Bonificacion.Remove(bonificacion);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Bonificación eliminada.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al eliminar bonificación: " + ex.Message;
            }
            return response;
        }

        public async Task<IEnumerable<Bonificacion>> ObtenerBonificacionesPorPlanillaAsync(int idPlanilla)
        {
            return await _context.Bonificacion
                .Where(b => b.Id_Planilla == idPlanilla)
                .OrderByDescending(b => b.Fecha)
                .ToListAsync();
        }
    }
}
