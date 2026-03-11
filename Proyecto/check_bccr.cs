using System;
using System.Net.Http;
using System.Xml;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJCQ0NSLVNEREUiLCJzdWIiOiJybWhib3JzaWdAZ21haWwuY29tIiwiYXVkIjoiU0RERS1TaXRpb0V4dGVybm8iLCJleHAiOjI1MzQwMjMwMDgwMCwibmJmIjoxNzczMTQ5MzExLCJpYXQiOjE3NzMxNDkzMTEsImp0aSI6ImViZDljMmNmLTRhMWEtNDRlNC05ZGU4LWU2MmU0NzI0MWExNiIsImVtYWlsIjoicm1oYm9yc2lnQGdtYWlsLmNvbSJ9.6b-gXX7j7szBBlOlhB5ddMieIEWOrja7QYpFS9CQf_0";
        string email = "rmhborsig@gmail.com";
        string nombre = "VentaFacil";
        string code = "318";
        string date = DateTime.Now.ToString("dd/MM/yyyy");
        
        string urlBase = "https://gee.bccr.fi.cr/Indicadores/Suscripciones/WS/wsindicadoreseconomicos.asmx/ObtenerIndicadoresEconomicosXML";
        string url = $"{urlBase}?Indicador={code}&FechaInicio={date}&FechaFinal={date}&Nombre={Uri.EscapeDataString(nombre)}&SubNiveles=N&CorreoElectronico={Uri.EscapeDataString(email)}&Token={token}";

        Console.WriteLine($"URL: {url}");
        
        using var _httpClient = new HttpClient();
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode) {
            Console.WriteLine($"Error HTTP: {response.StatusCode}");
            return;
        }

        string xmlResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Respuesta RAW:");
        Console.WriteLine(xmlResponse);
    }
}
