using System.Threading.Tasks;

namespace VentaFacil.web.Services.BCCR
{
    public interface IBccrService
    {
        Task<(decimal Compra, decimal Venta)> ObtenerTipoDeCambioDelDiaAsync();
    }
}
