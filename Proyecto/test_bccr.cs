using System;
using System.Net.Http;
using System.Xml;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJCQ0NSLVNEREUiLCJzdWIiOiJybWhib3JzaWdAZ21haWwuY29tIiwiYXVkIjoiU0RERS1TaXRpb0V4dGVybm8iLCJleHAiOjI1MzQwMjMwMDgwMCwibmJmIjoxNzczMTQ5MzExLCJpYXQiOjE3NzMxNDkzMTEsImp0aSI6ImViZDljMmNmLTRhMWEtNDRlNC05ZGU4LWU2MmU0NzI0MWExNiIsImVtYWlsIjoicm1oYm9yc2lnQGdtYWlsLmNvbSJ9.6b-gXX7j7szBBlOlhB5ddMieIEWOrja7QYpFS9CQf_0";
        string email = "rmhborsig@gmail.com";
        string nombre = "VentaFacil";
        string url = $"https://gee.bccr.fi.cr/Indicadores/Suscripciones/WS/wsindicadoreseconomicos.asmx/ObtenerIndicadoresEconomicosXML?Indicador=318&FechaInicio=10/03/2026&FechaFinal=10/03/2026&Nombre={Uri.EscapeDataString(nombre)}&SubNiveles=N&CorreoElectronico={Uri.EscapeDataString(email)}&Token={token}";

        using HttpClient client = new HttpClient();
        var response = await client.GetAsync(url);
        string xmlResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine("---- RESPONSE ----");
        Console.WriteLine(xmlResponse);
        
        try {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlResponse);
            
            if (doc.DocumentElement != null && doc.DocumentElement.Name == "string")
            {
                Console.WriteLine("Found root element String. Inner Text:");
                Console.WriteLine(doc.DocumentElement.InnerText);

                XmlDocument innerDoc = new XmlDocument();
                innerDoc.LoadXml(doc.DocumentElement.InnerText);
                XmlNode nodoValor = innerDoc.SelectSingleNode("//NUM_VALOR");
                if (nodoValor != null) {
                    Console.WriteLine("Parsed value (Nested): " + nodoValor.InnerText);
                } else {
                     Console.WriteLine("Inner NUM_VALOR is null");
                     
                     // Try with namespace manager
                     XmlNamespaceManager nsmgr = new XmlNamespaceManager(innerDoc.NameTable);
                     // Usually DataSet adds standard namespaces
                     XmlNode nodoValorNS = innerDoc.GetElementsByTagName("NUM_VALOR")[0];
                     if (nodoValorNS != null) {
                         Console.WriteLine("Parsed with TagName: " + nodoValorNS.InnerText);
                     }
                }
            } else {
                Console.WriteLine("Root element is not 'string'");
                XmlNode nodoDirecto = doc.SelectSingleNode("//NUM_VALOR");
                if (nodoDirecto != null) {
                    Console.WriteLine("Parsed value (Direct): " + nodoDirecto.InnerText);
                } else {
                    XmlNode nodoValorNS = doc.GetElementsByTagName("NUM_VALOR")[0];
                    if (nodoValorNS != null) {
                        Console.WriteLine("Parsed with TagName: " + nodoValorNS.InnerText);
                    }
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
