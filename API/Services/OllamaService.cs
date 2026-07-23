using System.Text;
using System.Text.Json;

namespace API.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public OllamaService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
            _model = config["Ollama:Model"] ?? "qwen2.5:7b";
        }

        public async Task<string> GenerateResponseAsync(string prompt, string systemPrompt = null)
        {
            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                system = systemPrompt ?? "You are a helpful AI assistant in a dating application.",
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                return responseProp.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}