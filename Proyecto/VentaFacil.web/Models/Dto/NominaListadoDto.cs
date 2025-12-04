public class NominaListadoDto
{
    public int Id_Nomina { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFinal { get; set; }

    public string Estado { get; set; } = string.Empty;

    // Propiedad calculada para mostrar en el Select
    public string Descripcion => $"{FechaInicio:dd/MM/yyyy} - {FechaFinal:dd/MM/yyyy} ({Estado})";
}
