namespace VentaFacil.web.Models.Dto
{
    public class ResumenPedidosDto
    {
        public int TotalBorrador { get; set; }
        public int TotalEnCocina { get; set; }
        public int TotalListos { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalCancelados { get; set; }
    }
}
