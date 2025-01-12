using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Ollama API URL'si ve model adı
        string ollamaUrl = "http://localhost:11434/api/chat"; // Ollama'nın varsayılan localhost URL'si
        string modelName = "llama3.2:1b"; // Kullanılacak model

        // Kullanıcıdan alınacak rota (örnek veri)
        var route = new[]
        {
            new { x = 5.0, y = 3.0 },
            new { x = 2.0, y = 1.0 },
            new { x = 8.0, y = 6.0 },
            new { x = 4.0, y = 4.0 }
        };

        // Llama3.2:1b ile rota optimizasyonu
        var optimizedRoute = await OptimizeRouteAsync(ollamaUrl, modelName, route);

        // Sonuçları yazdır
        Console.WriteLine("Düzeltilmiş Rota:");
        Console.WriteLine(JsonSerializer.Serialize(optimizedRoute, new JsonSerializerOptions { WriteIndented = true }));
    }

    static async Task<object> OptimizeRouteAsync(string apiUrl, string modelName, object route)
    {
        using var httpClient = new HttpClient();

        // Ollama'ya gönderilecek istek
        var payload = new
        {
            model = modelName,
            prompt = $"Optimize this flight route: {JsonSerializer.Serialize(route)}"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            // POST isteği gönder
            var response = await httpClient.PostAsync(apiUrl, requestContent);
            response.EnsureSuccessStatusCode();

            // Yanıtı işle
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<dynamic>(responseContent);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            return null;
        }
    }
}
