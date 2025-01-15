using AIOutlierDetection;
using FlightRouteOptimizer;
using static FlightRouteOptimizer.OllamaService;

class Program
{
    static async Task Main(string[] args)
    {
        var service = new OllamaService();
        var request = new RouteRequest
        {
            Points = new List<RoutePoint>
            {
                new RoutePoint { Latitude = 41.0082, Longitude = 28.9784, Altitude = 1000 },
                new RoutePoint { Latitude = 39.9334, Longitude = 32.8597, Altitude = 1500 }
            },
            Constraints = new Dictionary<string, object>
            {
                { "maxAltitude", 2000 },
                { "minAltitude", 500 }
            }
        };

        //var optimizedRoute = await service.OptimizeRouteAsync(request);

        // Detector örneği oluştur
        var detector = new OllamaOutlierDetector();

        // Örnek veri oluştur
        var data = new List<OllamaOutlierDetector.Point>
        {
            new OllamaOutlierDetector.Point
            {
                Timestamp = DateTime.Now.AddHours(-4).ToString(),
                Value = 10.5
            },
            new OllamaOutlierDetector.Point
            {
                Timestamp = DateTime.Now.AddHours(-3).ToString(),
                Value = 11.2
            },
            new OllamaOutlierDetector.Point
            {
                Timestamp = DateTime.Now.AddHours(-2).ToString(),
                Value = 150.0  // Muhtemel outlier
            },
            new OllamaOutlierDetector.Point
            {
                Timestamp = DateTime.Now.AddHours(-1).ToString(),
                Value = 10.8
            }
        };

        // Opsiyonel parametreler
        var parameters = new Dictionary<string, object>
        {
            { "sensitivityThreshold", 0.95 },
            { "minimumAnomalyScore", 0.7 }
        };

        // Outlier tespiti yap
        var result = await detector.DetectOutliersAsync(data, parameters);

        // Sonuçları görüntüle
        //Console.WriteLine($"Analiz özeti: {result.AnalysisSummary}");

        foreach (var point in result.Points)
        {
            Console.WriteLine($"Timestamp: {point.Timestamp}");
            Console.WriteLine($"Value: {point.Value}");
            Console.WriteLine($"Is Outlier: {point.IsOutlier}");
            Console.WriteLine($"Anomaly Score: {point.AnomalyScore:F2}");
            Console.WriteLine($"Explanation: {point.Explanation}");
            Console.WriteLine();
        }
    }
}
