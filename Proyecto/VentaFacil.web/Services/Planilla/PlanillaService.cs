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

                    if (dto.FechaFinal.Value.Date != dto.FechaInicio.Date)
                    {
                        response.Success = false;
                        response.Message = "Hora de salida inválida: La hora de salida debe ser el mismo día que la hora de entrada.";
                        return response;
                    }

                    if (dto.HoraInicioPausa.HasValue && dto.HoraInicioPausa.Value < dto.FechaInicio)
                    {
                        response.Success = false;
                        response.Message = "La hora de inicio de pausa no puede ser anterior a la hora de entrada.";
                        return response;
                    }

                    if (dto.HoraFinPausa.HasValue && dto.HoraInicioPausa.HasValue && dto.HoraFinPausa.Value < dto.HoraInicioPausa.Value)
                    {
                        response.Success = false;
                        response.Message = "La hora de fin de pausa no puede ser anterior al inicio de pausa.";
                        return response;
                    }

                    if (dto.HoraFinPausa.HasValue && dto.HoraFinPausa.Value > dto.FechaFinal.Value)
                    {
                        response.Success = false;
                        response.Message = "La hora de fin de pausa no puede ser posterior a la de salida.";
                        return response;
                    }
                }

                Models.Planilla planilla;
                // Obtener usuario para sacar tarifa
                var usuario = await _context.Usuario.FindAsync(dto.Id_Usr);
                if (usuario == null)
                {
                    response.Success = false;
                    response.Message = "Usuario no encontrado.";
                    return response;
                }

                var configuracion = await _context.ConfiguracionPlanilla.FirstOrDefaultAsync(c => c.Id_Usr == dto.Id_Usr);

                // Tarifa base (fallback a 2500m si es nula o no hay configuración)
                decimal tarifaPorHora = configuracion?.TarifaPorHora ?? 2500m;

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
                    planilla.HoraInicioPausa = dto.HoraInicioPausa;
                    planilla.HoraFinPausa = dto.HoraFinPausa;
                }
                else
                {
                    // Crear nueva planilla
                    planilla = new Models.Planilla
                    {
                        Id_Usr = dto.Id_Usr,
                        FechaInicio = dto.FechaInicio,
                        FechaFinal = dto.FechaFinal,
                        HoraInicioPausa = dto.HoraInicioPausa,
                        HoraFinPausa = dto.HoraFinPausa,
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
                    double horasPausa = 0;

                    if (planilla.HoraInicioPausa.HasValue && planilla.HoraFinPausa.HasValue)
                    {
                        TimeSpan duracionPausa = planilla.HoraFinPausa.Value - planilla.HoraInicioPausa.Value;
                        horasPausa = duracionPausa.TotalHours;
                    }

                    double horasEfectivas = duracion.TotalHours - horasPausa;
                    if (horasEfectivas < 0) horasEfectivas = 0;

                    planilla.HorasTrabajadas = (decimal)horasEfectivas;
                    planilla.EstadoRegistro = "Pendiente";
                    planilla.SalarioBruto = planilla.HorasTrabajadas * tarifaPorHora;
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

                // =====================================================================
                // VALIDACIÓN: Bloquear si la planilla ya está incluida en una nómina
                //             activa (cualquier estado distinto de "Anulada")
                // =====================================================================
                if (planilla.Id_Nomina.HasValue)
                {
                    var nominaAsociada = await _context.Nomina.FindAsync(planilla.Id_Nomina.Value);
                    if (nominaAsociada != null && nominaAsociada.Estado != "Anulada")
                    {
                        response.Success = false;
                        response.Message = $"No se pueden registrar horas extras: la jornada ya fue incorporada " +
                                           $"a la nómina #{nominaAsociada.Id_Nomina} (estado: {nominaAsociada.Estado}). " +
                                           $"Solo es posible en jornadas que aún no hayan sido procesadas en nómina.";
                        return response;
                    }
                }

                // Tarifa simulada para extras
                decimal tarifaExtra = 3500m;

                // Validar límite legal de horas extras: máximo 4 horas por jornada (Código de Trabajo CR)
                decimal maxHorasExtras = 4;
                if (dto.HorasExtras > maxHorasExtras)
                {
                    response.Success = false;
                    response.Message = "Horas extras exceden límite legal";
                    return response;
                }

                // =====================================================================
                // VALIDACIÓN: Turno del empleado (Modificado)
                // Verificar que las horas extras se registren en una jornada terminada.
                // =====================================================================
                if (dto.HorasExtras > 0)
                {
                    if (!planilla.FechaFinal.HasValue)
                    {
                        response.Success = false;
                        response.Message = "No se pueden registrar horas extras en una jornada sin hora de salida registrada. " +
                                           "Complete primero el registro de la jornada.";
                        return response;
                    }
                }

                planilla.HorasExtras = dto.HorasExtras;
                planilla.Bonificaciones = dto.MontoBonificaciones;

                // Recalcular Bruto: revertir extras/bonos previos y aplicar los nuevos
                decimal salarioBaseEstimado = planilla.SalarioBruto - (planilla.HorasExtras * tarifaExtra) - planilla.Bonificaciones;
                if (salarioBaseEstimado < 0) salarioBaseEstimado = planilla.HorasTrabajadas * 2500m;

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
                var query = _context.Planilla.Include(p => p.Usuario).AsQueryable();

                query = query.Where(p => p.FechaInicio >= dto.FechaInicio && p.FechaFinal <= dto.FechaFinal && p.Id_Nomina == null);

                if (dto.IncluirSoloUsuariosActivos)
                {
                    query = query.Where(p => p.Usuario.Estado); 
                }

                var planillasParaNomina = await query.ToListAsync();

                if (!planillasParaNomina.Any())
                {
                    response.Success = false;
                    response.Message = "No se encontraron registros de planilla para generar la nómina en este periodo.";
                    return response;
                }

                // VALIDACIÓN: Verificar si hay jornadas incompletas o pendientes de aprobación
                var jornadasIncompletas = planillasParaNomina.Where(p => p.EstadoRegistro == "Incompleta").ToList();
                var jornadasPendientes = planillasParaNomina.Where(p => p.EstadoRegistro == "Pendiente").ToList();
                
                if (jornadasIncompletas.Any() || jornadasPendientes.Any())
                {
                    if (jornadasIncompletas.Any())
                    {
                        var empleadosFaltantes = jornadasIncompletas
                            .Select(j => j.Usuario?.Nombre ?? $"ID: {j.Id_Usr}")
                            .Distinct()
                            .ToList();
                        foreach (var empleado in empleadosFaltantes)
                            response.ErroresValidacion.Add($"Empleado {empleado}: Tiene registros de jornada Incompletas (Falta registrar salida).");
                    }
                    if (jornadasPendientes.Any())
                    {
                        var empleadosPendientes = jornadasPendientes
                            .Select(j => j.Usuario?.Nombre ?? $"ID: {j.Id_Usr}")
                            .Distinct()
                            .ToList();
                        foreach (var empleado in empleadosPendientes)
                            response.ErroresValidacion.Add($"Empleado {empleado}: Tiene registros de jornada Pendientes (Falta aprobación).");
                    }

                    response.Success = false;
                    response.Message = "No se puede generar la nómina porque existen jornadas incompletas o pendientes de aprobación. Por favor revise los registros de los colaboradores.";
                    return response;
                }

                // Filtrar las rechazadas (no se pagan)
                planillasParaNomina = planillasParaNomina.Where(p => p.EstadoRegistro == "Completada" || p.EstadoRegistro == "Procesado").ToList();

                if (!planillasParaNomina.Any())
                {
                    response.Success = false;
                    response.Message = "No hay jornadas aprobadas para incluir en la nómina en este periodo.";
                    return response;
                }

                // =================================================================================
                // CÁLCULO AUTOMÁTICO DE DEDUCCIONES
                // =================================================================================

                // 1. Obtener Configuraciones
                var deduccionesLey = await _context.DeduccionLey.Where(d => d.Activo).ToListAsync();
                var tramosRenta = await _context.ImpuestoRenta.Where(i => i.Anio == 2025).OrderBy(i => i.LimiteInferior).ToListAsync();

                // ===========================================
                // PL-3002: Validar tasas configuradas
                // ===========================================
                var deduccionesSinTasa = deduccionesLey.Where(d => d.Porcentaje == 0).ToList();
                if (deduccionesSinTasa.Any())
                {
                    foreach (var ded in deduccionesSinTasa)
                    {
                        response.ErroresValidacion.Add(
                            $"Tasa de deducción '{ded.Nombre}' no definida (porcentaje = 0%). " +
                            $"Configure la tasa antes de procesar la nómina.");
                    }
                    response.Success = false;
                    response.Message = "No se puede generar la nómina: existen deducciones activas sin tasa configurada. Revise la configuración de deducciones.";
                    return response;
                }

                foreach (var planilla in planillasParaNomina)
                {
                    decimal salarioBruto = planilla.SalarioBruto;
                    decimal totalDeduccionesEmpleado = 0;

                    // ===========================================
                    // Escenario 3: Salario bruto = 0, omitir
                    // ===========================================
                    if (salarioBruto == 0)
                    {
                        planilla.Deducciones = 0;
                        planilla.SalarioNeto = 0;
                        planilla.Observaciones = "Sin deducción aplicada: el salario bruto es ₡0.00 para este período (sin horas registradas o tarifa no configurada).";
                        continue;
                    }

                    // Limpiar observaciones previas si el bruto es válido
                    planilla.Observaciones = null;

                    // 2. Calcular Cargas Sociales (SEM, IVM, LPT, etc.)
                    foreach (var ded in deduccionesLey)
                    {
                        decimal monto = salarioBruto * (ded.Porcentaje / 100m);
                        totalDeduccionesEmpleado += monto;
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

        public async Task<BaseResponse> RevertirNominaAsync(int idNomina, string justificacion)
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
                nomina.Observaciones = justificacion;

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

            var deduccionesLey = await _context.DeduccionLey
                .Where(d => d.Activo)
                .OrderBy(d => d.Nombre)
                .ToListAsync();

            var dto = new NominaDetalleDto
            {
                Id_Nomina    = nomina.Id_Nomina,
                FechaInicio  = nomina.FechaInicio,
                FechaFinal   = nomina.FechaFinal,
                FechaGeneracion = nomina.FechaGeneracion,
                Estado       = nomina.Estado,
                Observaciones = nomina.Observaciones,
                TotalBruto   = nomina.TotalBruto,
                TotalDeducciones = nomina.TotalDeducciones,
                TotalNeto    = nomina.TotalNeto,
                DeduccionesAplicadas = deduccionesLey.Select(d => new DeduccionResumenDto
                {
                    Nombre = d.Nombre,
                    Porcentaje = d.Porcentaje
                }).ToList(),
                Detalles = new List<PlanillaDetalleItemDto>()
            };

            foreach (var p in nomina.Planillas)
            {
                var item = new PlanillaDetalleItemDto
                {
                    NombreUsuario  = p.Usuario?.Nombre ?? $"ID {p.Id_Usr}",
                    Identificacion = p.Usuario?.Correo ?? "-",
                    HorasTrabajadas = p.HorasTrabajadas,
                    HorasExtras    = p.HorasExtras,
                    Bonificaciones = p.Bonificaciones,
                    SalarioBruto   = p.SalarioBruto,
                    Deducciones    = p.Deducciones,
                    SalarioNeto    = p.SalarioNeto,
                    Observaciones  = p.Observaciones,  // Nota explicativa (ej: bruto ₡0)
                    DeduccionesDetalle = new List<DeduccionDetalleItemDto>()
                };

                // Calcular desglose por deducción (₡0 si bruto es 0)
                foreach (var ded in deduccionesLey)
                {
                    item.DeduccionesDetalle.Add(new DeduccionDetalleItemDto
                    {
                        Nombre = ded.Nombre,
                        Porcentaje = ded.Porcentaje,
                        Monto = p.SalarioBruto * (ded.Porcentaje / 100m)
                    });
                }

                // Detalle diario: un Planilla = una jornada
                item.DiasLaborados.Add(new PlanillaDiaDto
                {
                    Id_Planilla      = p.Id_Planilla,
                    FechaInicio      = p.FechaInicio,
                    FechaFinal       = p.FechaFinal,
                    HoraInicioPausa  = p.HoraInicioPausa,
                    HoraFinPausa     = p.HoraFinPausa,
                    HorasTrabajadas  = p.HorasTrabajadas,
                    HorasExtras      = p.HorasExtras,
                    SalarioBruto     = p.SalarioBruto,
                    EstadoRegistro   = p.EstadoRegistro,
                    Id_Nomina        = p.Id_Nomina
                });

                dto.Detalles.Add(item);
            }

            return dto;
        }

        // ===================================================================
        // HISTORIAL LABORAL (5 AÑOS)
        // ===================================================================
        public async Task<HistorialLaboralResponse> ObtenerHistorialUsuarioAsync(int idUsuario, int pagina, int porPagina = 20)
        {
            var response = new HistorialLaboralResponse { PaginaActual = pagina, CantidadPorPagina = porPagina };

            try
            {
                var fechaLimite = DateTime.Now.AddYears(-5);

                var usuario = await _context.Usuario.FindAsync(idUsuario);
                if (usuario == null)
                {
                    response.Success = false;
                    response.Message = "Usuario no encontrado.";
                    return response;
                }

                response.Id_Usr = idUsuario;
                response.NombreUsuario = usuario.Nombre;

                var query = _context.Planilla
                    .Where(p => p.Id_Usr == idUsuario && p.FechaInicio >= fechaLimite)
                    .OrderByDescending(p => p.FechaInicio);

                response.TotalRegistros = await query.CountAsync();
                response.TotalPaginas   = (int)Math.Ceiling(response.TotalRegistros / (double)porPagina);

                var jornadas = await query
                    .Skip((pagina - 1) * porPagina)
                    .Take(porPagina)
                    .ToListAsync();

                response.Jornadas = jornadas.Select(p => new PlanillaDiaDto
                {
                    Id_Planilla     = p.Id_Planilla,
                    FechaInicio     = p.FechaInicio,
                    FechaFinal      = p.FechaFinal,
                    HoraInicioPausa = p.HoraInicioPausa,
                    HoraFinPausa    = p.HoraFinPausa,
                    HorasTrabajadas = p.HorasTrabajadas,
                    HorasExtras     = p.HorasExtras,
                    SalarioBruto    = p.SalarioBruto,
                    EstadoRegistro  = p.EstadoRegistro,
                    Id_Nomina       = p.Id_Nomina
                }).ToList();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al obtener el historial: " + ex.Message;
            }

            return response;
        }
        // ===================================================================
        // APROBACIÓN DE HORAS
        // ===================================================================
        public async Task<IEnumerable<PlanillaListadoDto>> ObtenerPlanillasPendientesAsync()
        {
            var planillas = await _context.Planilla
                .Include(p => p.Usuario)
                .Where(p => p.EstadoRegistro == "Pendiente" || p.EstadoRegistro == "Incompleta")
                .Select(p => new PlanillaListadoDto
                {
                    Id_Planilla = p.Id_Planilla,
                    Id_Usr = p.Id_Usr,
                    FechaInicio = p.FechaInicio,
                    FechaFinal = p.FechaFinal,
                    NombreUsuario = p.Usuario.Nombre, 
                    EstadoRegistro = p.EstadoRegistro
                })
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();

            return planillas;
        }

        public async Task<BaseResponse> AprobarRechazarPlanillaAsync(int idPlanilla, string estado, string observaciones)
        {
            var response = new BaseResponse();
            try
            {
                var planilla = await _context.Planilla.FindAsync(idPlanilla);
                if (planilla == null)
                {
                    response.Success = false;
                    response.Message = "Planilla no encontrada.";
                    return response;
                }

                if (estado != "Completada" && estado != "Rechazada")
                {
                    response.Success = false;
                    response.Message = "Estado no válido.";
                    return response;
                }

                planilla.EstadoRegistro = estado;
                
                // Si hay una observación nueva, podemos concatenar o sobrescribir. Por ahora sobrescribimos si no es vacía
                if (!string.IsNullOrWhiteSpace(observaciones))
                {
                    // Si ya tenía observaciones, las agregamos con un separador
                    if (!string.IsNullOrWhiteSpace(planilla.Observaciones))
                    {
                        planilla.Observaciones += " | Resolución Admin: " + observaciones;
                    }
                    else
                    {
                        planilla.Observaciones = observaciones;
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Planilla marcada como {estado}.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error al actualizar estado: " + ex.Message;
            }

            return response;
        }
    }
}