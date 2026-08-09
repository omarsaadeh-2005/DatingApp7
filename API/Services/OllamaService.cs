using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace API.Services
{
    public class OllamaService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;

        public OllamaService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _model = config["Ollama:Model"] ?? "llava";
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null)
        {
            var url = $"{_baseUrl.TrimEnd('/')}/api/generate";
            var fullPrompt = string.IsNullOrEmpty(systemPrompt) ? prompt : $"{systemPrompt}\n\n{prompt}";

            var requestBody = new
            {
                model = _model,
                prompt = fullPrompt,
                stream = false,
                format = "json" // forces Ollama to constrain output to valid JSON when the model supports it
            };

            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Ollama Error]: {response.StatusCode} - {responseString}");
                    return string.Empty;
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                if (jsonDoc.RootElement.TryGetProperty("response", out var responseProp))
                {
                    return SanitizeJson(responseProp.GetString() ?? string.Empty);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ollama Exception]: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<string> ProcessImageAsync(string base64Image, string mimeType, string prompt)
        {
            var url = $"{_baseUrl.TrimEnd('/')}/api/generate";
            if (base64Image.Contains(",")) base64Image = base64Image.Split(',')[1];
            base64Image = base64Image.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();

            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                images = new[] { base64Image },
                stream = false,
                format = "json"
            };

            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Ollama Image Error]: {response.StatusCode} - {responseString}");
                    return string.Empty;
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                if (jsonDoc.RootElement.TryGetProperty("response", out var responseProp))
                {
                    return SanitizeJson(responseProp.GetString() ?? string.Empty);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ollama Image Exception]: {ex.Message}");
                return string.Empty;
            }
        }

        private static string SanitizeJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = Regex.Replace(input, @"```(json)?", "", RegexOptions.IgnoreCase).Trim();

            int firstBrace = input.IndexOf('{');
            int lastBrace = input.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                return input.Substring(firstBrace, lastBrace - firstBrace + 1);

            int firstBracket = input.IndexOf('[');
            int lastBracket = input.LastIndexOf(']');
            if (firstBracket >= 0 && lastBracket > firstBracket)
                return input.Substring(firstBracket, lastBracket - firstBracket + 1);

            return input.Trim();
        }
    }
}