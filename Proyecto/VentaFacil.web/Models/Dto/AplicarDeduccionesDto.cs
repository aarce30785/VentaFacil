namespace VentaFacil.web.Models.Dto
{
    public class AplicarDeduccionesDto
    {
        public int Id_Nomina { get; set; }
        public decimal PorcentajeCCSS { get; set; }
        public decimal PorcentajeImpuestoRenta { get; set; }
        public bool RecalcularSalariosNetos { get; set; }
    }
}