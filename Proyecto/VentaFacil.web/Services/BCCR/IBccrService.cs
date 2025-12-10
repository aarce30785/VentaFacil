using System.Data;
using System.Threading.Tasks;

namespace VentaFacil.web.Services.BCCR
{
    public interface IBccrService
    {
        Task<DataSet> ObtenerIndicadoresEconomicosAsync(string codigoIndicador, string fechaInicio, string fechaFinal);
        Task<(decimal Compra, decimal Venta)> ObtenerTipoDeCambioDelDiaAsync();
    }
}
