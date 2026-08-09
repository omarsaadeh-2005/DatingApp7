using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace API.Services
{
    public class GeminiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string Model = "gemini-2.5-flash";

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        }

        public async Task<string> GenerateTextAsync(string prompt, string systemPrompt = null)
        {
            if (string.IsNullOrEmpty(_apiKey)) return string.Empty;

            var fullPrompt = string.IsNullOrEmpty(systemPrompt) ? prompt : $"{systemPrompt}\n\n{prompt}";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey.Trim()}";
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = fullPrompt } } } } };

            return await SendAsync(url, requestBody, "[Gemini Text");
        }

        public async Task<string> ProcessImageAsync(string base64Image, string mimeType, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                Console.WriteLine("[Gemini Error]: Missing Gemini API Key in configuration.");
                return string.Empty;
            }

            if (base64Image.Contains(",")) base64Image = base64Image.Split(',')[1];
            base64Image = base64Image.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim();

            var validMimeType = (mimeType?.Equals("image/png", StringComparison.OrdinalIgnoreCase) == true ||
                                  mimeType?.Equals("image/webp", StringComparison.OrdinalIgnoreCase) == true)
                                  ? mimeType : "image/jpeg";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey.Trim()}";
            var requestBody = new
            {
                contents = new[] {
                    new { parts = new object[] {
                        new { text = prompt },
                        new { inline_data = new { mime_type = validMimeType, data = base64Image } }
                    }}
                }
            };

            return await SendAsync(url, requestBody, "[Gemini Image");
        }

        private async Task<string> SendAsync(string url, object requestBody, string logPrefix)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                request.Headers.Authorization = null;

                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{logPrefix} Error]: {response.StatusCode} - {responseString}");
                    return string.Empty;
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                var text = jsonDoc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

                return SanitizeJson(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{logPrefix} Exception]: {ex.Message}");
                return string.Empty;
            }
        }

        // Strips markdown fences and extracts the first valid {...} or [...] block,
        // exactly like OllamaService does — keeps both providers behaving identically.
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