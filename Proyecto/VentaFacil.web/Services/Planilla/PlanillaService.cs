using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;
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
                // Validaciones
                if (dto.FechaFinal.HasValue)
                {
                    if (dto.FechaFinal.Value < dto.FechaInicio)
                    {
                        response.Success = false;
                        response.Message = "Hora de salida inválida: La hora de salida no puede ser anterior a la de entrada.";
                        return response;
                    }
                }

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
                        HorasExtras = 0,
                        Bonificaciones = 0,
                        Deducciones = 0,
                        SalarioNeto = 0
                    };
                    _context.Planilla.Add(planilla);
                }

                // Cálculo de horas y estado
                if (planilla.FechaFinal.HasValue)
                {
                    TimeSpan duracion = planilla.FechaFinal.Value - planilla.FechaInicio;
                    planilla.HorasTrabajadas = (int)duracion.TotalHours;
                    planilla.EstadoRegistro = "Completada";
                    planilla.SalarioBruto = (decimal)duracion.TotalHours * tarifaPorHora;
                }
                else
                {
                    planilla.HorasTrabajadas = 0;
                    planilla.EstadoRegistro = "Incompleta";
                    planilla.SalarioBruto = 0;
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = planilla.EstadoRegistro == "Incompleta" 
                    ? "Jornada registrada como incompleta." 
                    : "Jornada registrada correctamente.";
                
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

                // Validar límite legal de horas extras (Ej: Máximo 4 horas por día o 12 horas totales de jornada)
                // Asumimos un máximo de 4 horas extras por jornada para este ejemplo
                decimal maxHorasExtras = 4;
                if (dto.HorasExtras > maxHorasExtras)
                {
                    response.Success = false;
                    response.Message = "Horas extras exceden límite legal (Máximo 4 horas permitidas).";
                    return response;
                }

                // Validar horario (Turno)
                // Si el usuario tiene turno definido, verificar que las extras sean fuera de ese turno
                // Esta validación es compleja si no tenemos la fecha/hora exacta de las extras, 
                // pero podemos validar que si hay extras, la jornada total (Trabajadas + Extras) sea coherente.
                
                // Cargar usuario para ver turno
                var usuario = await _context.Usuario.FindAsync(planilla.Id_Usr);
                if (usuario != null && usuario.HoraEntrada.HasValue && usuario.HoraSalida.HasValue)
                {
                    // Lógica opcional: Si se quisiera validar contra el reloj
                }

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

                query = query.Where(p => p.FechaInicio >= dto.FechaInicio && p.FechaFinal <= dto.FechaFinal && p.Id_Nomina == null);

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

                // VALIDACIÓN: Verificar si hay jornadas incompletas
                var jornadasIncompletas = planillasParaNomina.Where(p => p.EstadoRegistro == "Incompleta").ToList();
                if (jornadasIncompletas.Any())
                {
                    response.Success = false;
                    response.Message = "No se puede generar la nómina porque existen jornadas incompletas. Por favor revise los registros de los colaboradores.";
                    return response;
                }

                // =================================================================================
                // CÁLCULO AUTOMÁTICO DE DEDUCCIONES
                // =================================================================================
                
                // 1. Obtener Configuraciones
                var deduccionesLey = await _context.DeduccionLey.Where(d => d.Activo).ToListAsync();
                var tramosRenta = await _context.ImpuestoRenta.Where(i => i.Anio == 2025).OrderBy(i => i.LimiteInferior).ToListAsync();

                foreach (var planilla in planillasParaNomina)
                {
                    decimal salarioBruto = planilla.SalarioBruto;
                    decimal totalDeduccionesEmpleado = 0;

                    // 2. Calcular Cargas Sociales (SEM, IVM, LPT, etc.)
                    foreach (var ded in deduccionesLey)
                    {
                        if (ded.Porcentaje > 0)
                        {
                            decimal monto = salarioBruto * (ded.Porcentaje / 100m);
                            totalDeduccionesEmpleado += monto;
                        }
                    }

                    // 3. Calcular Impuesto de Renta
                    decimal salarioNetoPreRenta = salarioBruto - totalDeduccionesEmpleado;
                    decimal impuestoRenta = 0;

                    if (tramosRenta.Any())
                    {
                        foreach (var tramo in tramosRenta)
                        {
                            if (salarioNetoPreRenta > tramo.LimiteInferior)
                            {
                                decimal baseCalculo = 0;
                                if (tramo.LimiteSuperior.HasValue)
                                {
                                    // Tramo intermedio
                                    if (salarioNetoPreRenta > tramo.LimiteSuperior.Value)
                                    {
                                        baseCalculo = tramo.LimiteSuperior.Value - tramo.LimiteInferior;
                                    }
                                    else
                                    {
                                        baseCalculo = salarioNetoPreRenta - tramo.LimiteInferior;
                                    }
                                }
                                else
                                {
                                    // Último tramo (Exceso de...)
                                    baseCalculo = salarioNetoPreRenta - tramo.LimiteInferior;
                                }

                                if (baseCalculo > 0)
                                {
                                    impuestoRenta += baseCalculo * (tramo.Porcentaje / 100m);
                                }
                            }
                        }
                    }

                    totalDeduccionesEmpleado += impuestoRenta;

                    planilla.Deducciones = totalDeduccionesEmpleado;
                    planilla.SalarioNeto = salarioBruto - totalDeduccionesEmpleado;
                }

                // =================================================================================
                // CREACIÓN DE NÓMINA
                // =================================================================================

                // Crear Encabezado de Nómina
                var nomina = new Nomina
                {
                    FechaInicio = dto.FechaInicio,
                    FechaFinal = dto.FechaFinal,
                    FechaGeneracion = DateTime.Now,
                    Estado = "Generada",
                    TotalBruto = planillasParaNomina.Sum(p => p.SalarioBruto),
                    TotalDeducciones = planillasParaNomina.Sum(p => p.Deducciones),
                    TotalNeto = planillasParaNomina.Sum(p => p.SalarioNeto)
                };

                _context.Nomina.Add(nomina);
                await _context.SaveChangesAsync(); // Para obtener el Id_Nomina

                // Asignar Id_Nomina a las planillas
                foreach (var item in planillasParaNomina)
                {
                    item.Id_Nomina = nomina.Id_Nomina;
                    item.EstadoRegistro = "Procesado";
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Nómina generada exitosamente (Deducciones aplicadas automáticamente).";
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

        public async Task<BaseResponse> RevertirNominaAsync(int idNomina)
        {
            var response = new BaseResponse();
            try
            {
                var nomina = await _context.Nomina
                    .Include(n => n.Planillas)
                    .FirstOrDefaultAsync(n => n.Id_Nomina == idNomina);

                if (nomina == null)
                {
                    response.Success = false;
                    response.Message = "Nómina no encontrada.";
                    return response;
                }

                if (nomina.Estado == "Anulada")
                {
                    response.Success = false;
                    response.Message = "La nómina ya se encuentra anulada.";
                    return response;
                }

                // Cambiar estado de la nómina
                nomina.Estado = "Anulada";

                // Liberar las planillas asociadas
                foreach (var planilla in nomina.Planillas)
                {
                    planilla.Id_Nomina = null;
                    planilla.EstadoRegistro = "Completada"; // Regresar a estado previo
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Nómina revertida/anulada correctamente. Las jornadas han sido liberadas.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al revertir la nómina: " + ex.Message;
            }
            return response;
        }

        public async Task<NominaConsultaResponse> ConsultarNominasAsync(NominaConsultaDto filtros)
        {
            var response = new NominaConsultaResponse();

            try
            {
                // Valores por defecto
                int pagina = filtros.Pagina <= 0 ? 1 : filtros.Pagina;
                int cantidad = filtros.CantidadPorPagina <= 0 ? 10 : filtros.CantidadPorPagina;

                var query = _context.Nomina
                    .Include(n => n.Planillas)
                    .ThenInclude(p => p.Usuario)
                    .AsQueryable();

                // Filtros
                if (filtros.FechaInicio.HasValue)
                    query = query.Where(n => n.FechaInicio >= filtros.FechaInicio.Value);

                if (filtros.FechaFinal.HasValue)
                    query = query.Where(n => n.FechaFinal <= filtros.FechaFinal.Value);

                // Filtro por Tipo de Periodo
                if (!string.IsNullOrEmpty(filtros.TipoPeriodo))
                {
                    DateTime now = DateTime.Now;
                    if (filtros.TipoPeriodo == "Mensual")
                    {
                        var inicioMes = new DateTime(now.Year, now.Month, 1);
                        query = query.Where(n => n.FechaInicio >= inicioMes);
                    }
                    else if (filtros.TipoPeriodo == "Trimestral")
                    {
                        var inicioTrimestre = now.AddMonths(-3);
                        query = query.Where(n => n.FechaInicio >= inicioTrimestre);
                    }
                    else if (filtros.TipoPeriodo == "Anual")
                    {
                        var inicioAnio = new DateTime(now.Year, 1, 1);
                        query = query.Where(n => n.FechaInicio >= inicioAnio);
                    }
                }

                if (!string.IsNullOrEmpty(filtros.Estado))
                    query = query.Where(n => n.Estado == filtros.Estado);

                if (filtros.Id_Usr.HasValue)
                    query = query.Where(n => n.Planillas.Any(p => p.Id_Usr == filtros.Id_Usr.Value));

                // Filtro por Nombre/Correo de Usuario
                if (!string.IsNullOrEmpty(filtros.BusquedaUsuario))
                {
                    query = query.Where(n => n.Planillas.Any(p => 
                        p.Usuario.Nombre.Contains(filtros.BusquedaUsuario) || 
                        p.Usuario.Correo.Contains(filtros.BusquedaUsuario)));
                }

                // Total
                int totalRegistros = await query.CountAsync();

                // Si no hay registros
                if (totalRegistros == 0)
                {
                    response.Success = true;
                    response.Message = "Consulta exitosa (sin resultados).";
                    response.Nominas = new List<Nomina>();
                    response.PaginaActual = 1;
                    response.TotalPaginas = 1;
                    return response;
                }

                int totalPaginas = (int)Math.Ceiling((double)totalRegistros / cantidad);

                var nominas = await query
                    .OrderByDescending(n => n.FechaGeneracion)
                    .Skip((pagina - 1) * cantidad)
                    .Take(cantidad)
                    .AsNoTracking()
                    .ToListAsync();

                response.Success = true;
                response.Message = "Consulta exitosa.";
                response.Nominas = nominas;
                response.PaginaActual = pagina;
                response.TotalPaginas = totalPaginas;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al consultar nóminas: " + ex.Message;
                response.Nominas = new List<Nomina>();
                response.PaginaActual = 1;
                response.TotalPaginas = 1;
            }

            return response;
        }

        public async Task<IEnumerable<PlanillaListadoDto>> ObtenerPlanillasParaExtrasAsync()
        {
            var planillas = await _context.Planilla
                .Include(p => p.Usuario)
                .Select(p => new PlanillaListadoDto
                {
                    Id_Planilla = p.Id_Planilla,
                    Id_Usr = p.Id_Usr,
                    FechaInicio = p.FechaInicio,
                    FechaFinal = p.FechaFinal,
                    NombreUsuario = p.Usuario.Nombre, 
                    EstadoRegistro = p.EstadoRegistro
                })
                .OrderByDescending(p => p.Id_Planilla)
                .ToListAsync();

            return planillas;
        }

        public async Task<IEnumerable<NominaListadoDto>> ObtenerNominasGeneradasAsync()
        {
            return await _context.Nomina
                .Where(n => n.Estado == "Generada")
                .OrderByDescending(n => n.Id_Nomina)
                .Select(n => new NominaListadoDto
                {
                    Id_Nomina = n.Id_Nomina,
                    FechaInicio = n.FechaInicio,
                    FechaFinal = n.FechaFinal,
                    Estado = n.Estado
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<VentaFacil.web.Models.Usuario>> ObtenerUsuariosAsync()
        {
            return await _context.Usuario
                .Where(u => u.Estado)
                .OrderBy(u => u.Nombre)
                .ToListAsync();
        }

        public async Task<NominaDetalleDto> ObtenerDetalleNominaParaExportarAsync(int idNomina)
        {
            var nomina = await _context.Nomina
                .Include(n => n.Planillas)
                .ThenInclude(p => p.Usuario)
                .FirstOrDefaultAsync(n => n.Id_Nomina == idNomina);

            if (nomina == null) return null;

            var dto = new NominaDetalleDto
            {
                Id_Nomina = nomina.Id_Nomina,
                FechaInicio = nomina.FechaInicio,
                FechaFinal = nomina.FechaFinal,
                FechaGeneracion = nomina.FechaGeneracion,
                Estado = nomina.Estado,
                TotalBruto = nomina.TotalBruto,
                TotalDeducciones = nomina.TotalDeducciones,
                TotalNeto = nomina.TotalNeto,
                Detalles = nomina.Planillas.Select(p => new PlanillaDetalleItemDto
                {
                    NombreUsuario = p.Usuario.Nombre,
                    Identificacion = p.Usuario.Correo, // Usando correo como ID secundario
                    HorasTrabajadas = p.HorasTrabajadas,
                    HorasExtras = p.HorasExtras,
                    Bonificaciones = p.Bonificaciones,
                    SalarioBruto = p.SalarioBruto,
                    Deducciones = p.Deducciones,
                    SalarioNeto = p.SalarioNeto
                }).ToList()
            };

            return dto;
        }
    }
}