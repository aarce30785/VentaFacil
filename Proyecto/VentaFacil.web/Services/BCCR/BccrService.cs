using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace VentaFacil.web.Services.BCCR
{
    public class BccrService : IBccrService
    {
        private readonly BccrSettings _settings;

        public BccrService(IOptions<BccrSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<DataSet> ObtenerIndicadoresEconomicosAsync(string codigoIndicador, string fechaInicio, string fechaFinal)
        {
            // Configure the proxy client
            var client = new wsindicadoreseconomicosSoapClient(
                wsindicadoreseconomicosSoapClient.EndpointConfiguration.wsindicadoreseconomicosSoap);
            
            // Should properly set endpoint address if needed, but default constructor usually takes from generated config or defaults
            // If the URL in settings is different, we can set it:
            if (!string.IsNullOrEmpty(_settings.Url))
            {
               client.Endpoint.Address = new System.ServiceModel.EndpointAddress(_settings.Url);
            }

            try
            {
                var xmlResult = await client.ObtenerIndicadoresEconomicosXMLAsync(
                    Indicador: codigoIndicador, 
                    FechaInicio: fechaInicio, 
                    FechaFinal: fechaFinal, 
                    Nombre: _settings.Nombre, 
                    SubNiveles: "N", 
                    CorreoElectronico: _settings.Email, 
                    Token: _settings.Token
                );

                var dataSet = new DataSet();
                if (!string.IsNullOrEmpty(xmlResult))
                {
                    using (var reader = new System.IO.StringReader(xmlResult))
                    {
                        dataSet.ReadXml(reader);
                    }
                }
                
                return dataSet;
            }
            finally
            {
                if (client.State == System.ServiceModel.CommunicationState.Opened)
                {
                    await client.CloseAsync();
                }
                else
                {
                    client.Abort(); // Ensure resources are released if in faulted state
                }
            }
        }
        public async Task<(decimal Compra, decimal Venta)> ObtenerTipoDeCambioDelDiaAsync()
        {
            string today = DateTime.Now.ToString("dd/MM/yyyy");
            
            // 317 = Compra, 318 = Venta
            var taskCompra = GetRateValueAsync("317", today);
            var taskVenta = GetRateValueAsync("318", today);

            await Task.WhenAll(taskCompra, taskVenta);

            return (await taskCompra, await taskVenta);
        }

        private async Task<decimal> GetRateValueAsync(string code, string date)
        {
            try
            {
                var data = await ObtenerIndicadoresEconomicosAsync(code, date, date);
                if (data != null && data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0)
                {
                    // Asumiendo que la columna de valor se llama "valor" o es la segunda columna
                    // BCCR devuelve: COD_INDICADOR, DES_FECHA, NUM_VALOR
                    // Pero el DataSet puede variar. Usualmente NUM_VALOR es el dato.
                    // Si el usuario dijo row["valor"], revisamos si existe.
                    var row = data.Tables[0].Rows[0];
                    if (data.Tables[0].Columns.Contains("NUM_VALOR"))
                    {
                        return Convert.ToDecimal(row["NUM_VALOR"]);
                    }
                    else if (data.Tables[0].Columns.Count >= 3) 
                    {
                         // Fallback index
                         return Convert.ToDecimal(row[2]);
                    }
                }
                return 0m;
            }
            catch
            {
                // Manejo silencioso o log de errores
                return 0m;
            }
        }
    }
}
