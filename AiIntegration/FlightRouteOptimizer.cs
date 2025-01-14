using System.Text;
using System.Text.Json;

namespace FlightRouteOptimizer
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;

        public OllamaService(string baseUrl = "http://localhost:11434", string model = "llama3.2:1b")
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _model = model;
        }

        public class RoutePoint
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Altitude { get; set; }
        }

        public class RouteRequest
        {
            public List<RoutePoint> Points { get; set; }
            public Dictionary<string, object> Constraints { get; set; }
        }

        public async Task<List<RoutePoint>> OptimizeRouteAsync(RouteRequest request)
        {
            try
            {
                // AI prompt oluştur
                string prompt = CreateRouteOptimizationPrompt(request);

                // Ollama request objesi
                var ollamaRequest = new
                {
                    model = _model,
                    prompt = prompt,
                    stream = false
                };

                // API isteği gönder
                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/generate",
                    new StringContent(JsonSerializer.Serialize(ollamaRequest), Encoding.UTF8, "application/json")
                );

                response.EnsureSuccessStatusCode();

                // Yanıtı parse et
                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

                // AI yanıtını route noktalarına dönüştür
                return ParseOptimizedRoute(ollamaResponse.Response);
            }
            catch (Exception ex)
            {
                throw new Exception("Route optimization failed", ex);
            }
        }

        private string CreateRouteOptimizationPrompt(RouteRequest request)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Please optimize this flight route considering these points:");

            foreach (var point in request.Points)
            {
                promptBuilder.AppendLine($"- Lat: {point.Latitude}, Long: {point.Longitude}, Alt: {point.Altitude}");
            }

            if (request.Constraints?.Any() == true)
            {
                promptBuilder.AppendLine("\nConstraints:");
                foreach (var constraint in request.Constraints)
                {
                    promptBuilder.AppendLine($"- {constraint.Key}: {constraint.Value}");
                }
            }

            promptBuilder.AppendLine("\nPlease return the optimized route as a JSON array of points with latitude, longitude, and altitude.");
            return promptBuilder.ToString();
        }

        private List<RoutePoint> ParseOptimizedRoute(string aiResponse)
        {
            try
            {
                // AI'dan gelen JSON yanıtını parse et
                return JsonSerializer.Deserialize<List<RoutePoint>>(aiResponse);
            }
            catch
            {
                // Eğer yanıt JSON formatında değilse, basit text parsing dene
                var points = new List<RoutePoint>();
                var lines = aiResponse.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("Lat:") && line.Contains("Long:"))
                    {
                        // Basic text parsing
                        var parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            points.Add(new RoutePoint
                            {
                                Latitude = ExtractNumber(parts[0]),
                                Longitude = ExtractNumber(parts[1]),
                                Altitude = ExtractNumber(parts[2])
                            });
                        }
                    }
                }
                return points;
            }
        }

        private double ExtractNumber(string text)
        {
            var numberString = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            return double.TryParse(numberString, out double result) ? result : 0;
        }

        private class OllamaResponse
        {
            public string Response { get; set; }
        }
    }
}