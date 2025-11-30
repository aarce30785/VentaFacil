using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Planilla;

namespace VentaFacil.web.Services.Planilla
{
    public class PlanillaService : IPlanillaService
    {
        private readonly ApplicationDbContext _context;

        public PlanillaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RegistrarHorasResponse> RegistrarHorasAsync(RegistrarHorasDto dto)
        {
            var response = new RegistrarHorasResponse();

            try
            {
                Models.Planilla planilla;

                // Tarifa base simulada para el cálculo (esto vendría de configuración o usuario en un caso real)
                decimal tarifaPorHora = 2500m;

                if (dto.Id_Planilla.HasValue && dto.Id_Planilla.Value > 0)
                {
                    planilla = await _context.Planilla.FirstOrDefaultAsync(p => p.Id_Planilla == dto.Id_Planilla.Value);
                    if (planilla == null)
                    {
                        response.Success = false;
                        response.Message = "La planilla indicada no existe.";
                        return response;
                    }

                    // Actualizar datos
                    planilla.HorasTrabajadas = dto.HorasTrabajadas;
                    planilla.FechaInicio = dto.FechaInicio;
                    planilla.FechaFinal = dto.FechaFinal;
                }
                else
                {
                    // Crear nueva planilla
                    planilla = new Models.Planilla
                    {
                        Id_Usr = dto.Id_Usr,
                        FechaInicio = dto.FechaInicio,
                        FechaFinal = dto.FechaFinal,
                        HorasTrabajadas = dto.HorasTrabajadas,
                        EstadoRegistro = "Pendiente",
                        HorasExtras = 0,
                        Bonificaciones = 0,
                        Deducciones = 0,
                        SalarioNeto = 0
                    };
                    _context.Planilla.Add(planilla);
                }

                // Cálculo simple de salario bruto base
                planilla.SalarioBruto = planilla.HorasTrabajadas * tarifaPorHora;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Horas registradas correctamente.";
                response.Id_Planilla = planilla.Id_Planilla;
                response.HorasTrabajadas = planilla.HorasTrabajadas;
                response.FechaInicio = planilla.FechaInicio;
                response.FechaFinal = planilla.FechaFinal;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al registrar horas: " + ex.Message;
            }

            return response;
        }

        public async Task<RegistrarExtrasBonosResponse> RegistrarExtrasBonosAsync(RegistrarExtrasBonosDto dto)
        {
            var response = new RegistrarExtrasBonosResponse();

            try
            {
                var planilla = await _context.Planilla.FirstOrDefaultAsync(p => p.Id_Planilla == dto.Id_Planilla);

                if (planilla == null)
                {
                    response.Success = false;
                    response.Message = "Registro de planilla no encontrado.";
                    return response;
                }

                // Tarifa simulada para extras
                decimal tarifaExtra = 3500m;

                planilla.HorasExtras = dto.HorasExtras;
                planilla.Bonificaciones = dto.MontoBonificaciones;

                // Recalcular Bruto: (Base ya existente) + (Extras * Tarifa) + Bonos
                // Nota: En un escenario real se recalcularía todo desde cero, aquí sumamos al bruto base implícito
                decimal salarioBaseEstimado = planilla.SalarioBruto - (planilla.HorasExtras * tarifaExtra) - planilla.Bonificaciones; // Revertir cálculo anterior si hubiera
                if (salarioBaseEstimado < 0) salarioBaseEstimado = planilla.HorasTrabajadas * 2500m; // Fallback

                planilla.SalarioBruto = salarioBaseEstimado + (planilla.HorasExtras * tarifaExtra) + planilla.Bonificaciones;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Extras y bonificaciones actualizadas.";
                response.Id_Planilla = planilla.Id_Planilla;
                response.HorasExtras = planilla.HorasExtras;
                response.Bonificaciones = planilla.Bonificaciones;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al registrar extras: " + ex.Message;
            }

            return response;
        }

        public async Task<GenerarNominaResponse> GenerarNominaAsync(GenerarNominaDto dto)
        {
            var response = new GenerarNominaResponse();

            try
            {
                // Buscar planillas en el rango que no tengan nómina asignada o sean pendientes
                var query = _context.Planilla.AsQueryable();

                query = query.Where(p => p.FechaInicio >= dto.FechaInicio && p.FechaFinal <= dto.FechaFinal);

                if (dto.IncluirSoloUsuariosActivos)
                {
                    // Asumiendo que Usuario tiene una propiedad Activo, si no existe, omitir esta línea
                    // query = query.Where(p => p.Usuario.Activo); 
                }

                var planillasParaNomina = await query.ToListAsync();

                if (!planillasParaNomina.Any())
                {
                    response.Success = false;
                    response.Message = "No se encontraron registros de planilla para generar la nómina en este periodo.";
                    return response;
                }

                // Crear Encabezado de Nómina
                var nomina = new Nomina
                {
                    FechaInicio = dto.FechaInicio,
                    FechaFinal = dto.FechaFinal,
                    FechaGeneracion = DateTime.Now,
                    Estado = "Generada",
                    TotalBruto = planillasParaNomina.Sum(p => p.SalarioBruto),
                    TotalDeducciones = planillasParaNomina.Sum(p => p.Deducciones),
                    TotalNeto = planillasParaNomina.Sum(p => p.SalarioNeto) // Inicialmente puede ser igual al bruto si no hay deducciones
                };

                _context.Nomina.Add(nomina);
                await _context.SaveChangesAsync(); // Para obtener el Id_Nomina

                // Asignar Id_Nomina a las planillas
                foreach (var item in planillasParaNomina)
                {
                    item.Id_Nomina = nomina.Id_Nomina;
                    item.EstadoRegistro = "Procesado";
                }

                // Recalcular totales por seguridad
                nomina.TotalBruto = planillasParaNomina.Sum(p => p.SalarioBruto);
                nomina.TotalDeducciones = planillasParaNomina.Sum(p => p.Deducciones);
                nomina.TotalNeto = planillasParaNomina.Sum(p => p.SalarioBruto) - nomina.TotalDeducciones;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Nómina generada exitosamente.";
                response.Id_Nomina = nomina.Id_Nomina;
                response.TotalBruto = nomina.TotalBruto;
                response.TotalDeducciones = nomina.TotalDeducciones;
                response.TotalNeto = nomina.TotalNeto;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al generar la nómina: " + ex.Message;
            }

            return response;
        }

        public async Task<AplicarDeduccionesResponse> AplicarDeduccionesAsync(AplicarDeduccionesDto dto)
        {
            var response = new AplicarDeduccionesResponse();

            try
            {
                var nomina = await _context.Nomina
                    .Include(n => n.Planillas)
                    .FirstOrDefaultAsync(n => n.Id_Nomina == dto.Id_Nomina);

                if (nomina == null)
                {
                    response.Success = false;
                    response.Message = "Nómina no encontrada.";
                    return response;
                }

                int registrosAfectados = 0;
                decimal totalDeduccionesAplicadas = 0;

                foreach (var planilla in nomina.Planillas)
                {
                    // Cálculo de deducciones
                    decimal montoCCSS = planilla.SalarioBruto * dto.PorcentajeCCSS;
                    decimal montoRenta = planilla.SalarioBruto * dto.PorcentajeImpuestoRenta;
                    decimal totalDeduccionLinea = montoCCSS + montoRenta;

                    planilla.Deducciones = totalDeduccionLinea;

                    if (dto.RecalcularSalariosNetos)
                    {
                        planilla.SalarioNeto = planilla.SalarioBruto - planilla.Deducciones;
                    }

                    registrosAfectados++;
                    totalDeduccionesAplicadas += totalDeduccionLinea;
                }

                // Actualizar totales de la nómina
                nomina.TotalDeducciones = nomina.Planillas.Sum(p => p.Deducciones);
                nomina.TotalNeto = nomina.Planillas.Sum(p => p.SalarioNeto);

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Deducciones aplicadas correctamente.";
                response.Id_Nomina = nomina.Id_Nomina;
                response.RegistrosAfectados = registrosAfectados;
                response.TotalDeduccionesAplicadas = totalDeduccionesAplicadas;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al aplicar deducciones: " + ex.Message;
            }

            return response;
        }

        public async Task<NominaConsultaResponse> ConsultarNominasAsync(NominaConsultaDto filtros)
        {
            var response = new NominaConsultaResponse();

            try
            {
                var query = _context.Nomina.AsQueryable();

                // Filtros
                if (filtros.FechaInicio.HasValue)
                {
                    query = query.Where(n => n.FechaInicio >= filtros.FechaInicio.Value);
                }

                if (filtros.FechaFinal.HasValue)
                {
                    query = query.Where(n => n.FechaFinal <= filtros.FechaFinal.Value);
                }

                if (!string.IsNullOrEmpty(filtros.Estado))
                {
                    query = query.Where(n => n.Estado == filtros.Estado);
                }

                if (filtros.Id_Usr.HasValue)
                {
                    query = query.Where(n => n.Planillas.Any(p => p.Id_Usr == filtros.Id_Usr.Value));
                }

                // Paginación
                int totalRegistros = await query.CountAsync();
                int totalPaginas = (int)Math.Ceiling((double)totalRegistros / filtros.CantidadPorPagina);

                var nominas = await query
                    .OrderByDescending(n => n.FechaGeneracion)
                    .Skip((filtros.Pagina - 1) * filtros.CantidadPorPagina)
                    .Take(filtros.CantidadPorPagina)
                    .AsNoTracking()
                    .ToListAsync();

                response.Success = true;
                response.Message = "Consulta exitosa.";
                response.Nominas = nominas;
                response.PaginaActual = filtros.Pagina;
                response.TotalPaginas = totalPaginas;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al consultar nóminas: " + ex.Message;
                response.Nominas = new List<Nomina>();
            }

            return response;
        }
    }
}