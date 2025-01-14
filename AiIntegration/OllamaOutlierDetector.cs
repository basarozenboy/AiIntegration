using System.Text;
using System.Text.Json;

namespace AIOutlierDetection
{
    public class OllamaOutlierDetector
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;

        public OllamaOutlierDetector(string baseUrl = "http://localhost:11434", string model = "llama3.2:1b")
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _model = model;
        }

        public async Task<Response> DetectOutliersAsync(List<Point> data, Dictionary<string, object> parameters = null)
        {
            try
            {
                // AI için prompt oluştur
                string prompt = CreateDetectionPrompt(data, parameters);

                // Ollama isteği oluştur
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

                var jsonDocument = JsonDocument.Parse(responseContent);
                var responseVal = jsonDocument.RootElement.GetProperty("response").ToString();

                var ollamaResponse = JsonSerializer.Deserialize<Root>(responseVal);

                // AI yanıtını işle
                return ParseAIResponse(data, ollamaResponse.Response);
            }
            catch (Exception ex)
            {
                throw new Exception("Outlier detection failed", ex);
            }
        }

        private string CreateDetectionPrompt(List<Point> data, Dictionary<string, object> parameters)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Please analyze this time series data for outliers and anomalies.");
            promptBuilder.AppendLine("Provide the following in your analysis:");
            promptBuilder.AppendLine("1. For each point, determine if it's an outlier");
            promptBuilder.AppendLine("2. Assign an anomaly score (0-1) to each point");
            promptBuilder.AppendLine("3. Provide a brief explanation for each outlier");
            promptBuilder.AppendLine("4. Include summary statistics");
            promptBuilder.AppendLine("\nData points:");

            foreach (var point in data)
            {
                promptBuilder.AppendLine($"Timestamp: {point.Timestamp}, Value: {point.Value}");
            }

            if (parameters?.Count > 0)
            {
                promptBuilder.AppendLine("\nParameters:");
                foreach (var param in parameters)
                {
                    promptBuilder.AppendLine($"{param.Key}: {param.Value}");
                }
            }

            promptBuilder.AppendLine("\nPlease format your response as JSON with the following structure:");
            promptBuilder.AppendLine(@"{
                ""points"": [
                    {
                        ""timestamp"": ""...'',
                        ""value"": number,
                        ""isOutlier"": boolean,
                        ""anomalyScore"": number,
                        ""explanation"": ""...""
                    }
                ],
                ""summary"": ""...'',
                ""statistics"": {
                    ""mean"": number,
                    ""stdDev"": number,
                    ""outlierCount"": number
                }
            }");

            return promptBuilder.ToString();
        }

        private Response ParseAIResponse(List<Point> originalData, Response result)
        {
            try
            {
                // Orijinal veriyle AI sonuçlarını birleştir
                if (result?.Points != null)
                {
                    for (int i = 0; i < Math.Min(originalData.Count, result.Points.Count); i++)
                    {
                        originalData[i].IsOutlier = result.Points[i].IsOutlier;
                        originalData[i].AnomalyScore = result.Points[i].AnomalyScore;
                        originalData[i].Explanation = result.Points[i].Explanation;
                    }
                }

                return new Response
                {
                    Points = originalData,
                    //AnalysisSummary = result?.AnalysisSummary,
                    //Statistics = result?.Statistics ?? new Dictionary<string, object>()
                };
            }
            catch
            {
                // JSON parse başarısız olursa basit text parsing dene
                return CreateBasicDetectionResult(originalData);
            }
        }

        private Response CreateBasicDetectionResult(List<Point> data)
        {
            // Basit metin analizi yaparak sonuç oluştur
            var summary = new StringBuilder();
            var statistics = new Dictionary<string, object>();

            foreach (var point in data)
            {
                // Değerin ortalamadan uzaklığına göre basit bir anomali skoru hesapla
                var mean = data.Average(p => p.Value);
                var stdDev = CalculateStandardDeviation(data.Select(p => p.Value).ToList());
                var zScore = Math.Abs((point.Value - mean) / stdDev);

                point.AnomalyScore = Math.Min(1, zScore / 3);
                point.IsOutlier = zScore > 3;
                point.Explanation = point.IsOutlier ? "Significant deviation from mean" : "Normal range";
            }

            return new Response
            {
                Points = data,
                //AnalysisSummary = "Basic statistical analysis performed",
                //Statistics = new Dictionary<string, object>
                //{
                //    { "outlierCount", data.Count(p => p.IsOutlier) }
                //}
            };
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            double mean = values.Average();
            double sumSquares = values.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumSquares / (values.Count - 1));
        }

        public class Point
        {
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }
            public bool IsOutlier { get; set; }
            public double AnomalyScore { get; set; }
            public string Explanation { get; set; }
        }

        public class Statistics
        {
            public double Mean { get; set; }
            public double StdDev { get; set; }
            public int OutlierCount { get; set; }
        }

        public class Response
        {
            public List<Point> Points { get; set; }
            public string Summary { get; set; }
            public Statistics Statistics { get; set; }
        }

        public class Root
        {
            public string Model { get; set; }
            public string CreatedAt { get; set; }
            public Response Response { get; set; }
            public bool Done { get; set; }
            public string DoneReason { get; set; }
        }
    }
}