using System;
using System.Net.Http;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace VentaFacil.web.Services.BCCR
{
    public class BccrService : IBccrService
    {
        private readonly IOptionsMonitor<BccrSettings> _settingsMonitor;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BccrService> _logger;

        public BccrService(IOptionsMonitor<BccrSettings> settingsMonitor, ILogger<BccrService> logger)
        {
            _settingsMonitor = settingsMonitor;
            _httpClient = new HttpClient();
            _logger = logger;
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
                var settings = _settingsMonitor.CurrentValue;

                // Validate if token is default so we avoid calling BCCR and timing out/erring out
                if (string.IsNullOrEmpty(settings.Token) || settings.Token.Contains("TU_TOKEN_AQUI"))
                {
                    _logger.LogWarning("⚠️ Token de BCCR inválido o no configurado. Debes agregarlo en appsettings.json. Retornando 0.");
                    return 0m;
                }

                // Base URL para obtener XML
                string urlBase = settings.Url.Replace(".asmx", ".asmx/ObtenerIndicadoresEconomicosXML");
                if (!settings.Url.Contains(".asmx")) urlBase = settings.Url; // Fallback 
                
                string url = $"{urlBase}?Indicador={code}&FechaInicio={date}&FechaFinal={date}&Nombre={Uri.EscapeDataString(settings.Nombre)}&SubNiveles=N&CorreoElectronico={Uri.EscapeDataString(settings.Email)}&Token={settings.Token}";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string xmlResponse = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlResponse);

                // La respuesta de ObtenerIndicadoresEconomicosXML del BCCR viene como un "<string xmlns=...> XML_ADENTRO </string>"
                if (doc.DocumentElement != null && doc.DocumentElement.Name == "string")
                {
                    XmlDocument innerDoc = new XmlDocument();
                    innerDoc.LoadXml(doc.DocumentElement.InnerText);
                    
                    // Extraer usando GetElementsByTagName para evadir problemas de Null Namespaces de BCCR
                    XmlNodeList nodosValor = innerDoc.GetElementsByTagName("NUM_VALOR");
                    if (nodosValor != null && nodosValor.Count > 0)
                    {
                        if (decimal.TryParse(nodosValor[0].InnerText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valor))
                        {
                            return valor;
                        }
                    }
                }
                
                // Intento directo en caso de que devuelva el DataSet raíz 
                XmlNodeList nodosDirectos = doc.GetElementsByTagName("NUM_VALOR");
                if (nodosDirectos != null && nodosDirectos.Count > 0)
                {
                    if (decimal.TryParse(nodosDirectos[0].InnerText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pd))
                    {
                        return pd;
                    }
                }

                _logger.LogWarning("No se encontró NUM_VALOR en la respuesta XML del BCCR para el indicador {Indicador}. Retornando 0.", code);
                return 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al conectar con el API del BCCR para el indicador {Indicador}", code);
                return 0m; 
            }
        }
    }
}
