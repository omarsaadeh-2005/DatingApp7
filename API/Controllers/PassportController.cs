using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace API.Controllers
{
    public class PassportController : BaseApiController
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public PassportController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        [HttpPost("process-passport")]
        public async Task<ActionResult<PassportDataDto>> ProcessPassport(IFormFile file, [FromForm] string validCitiesJson)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var geminiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(geminiKey)) 
                return BadRequest("Gemini API key is missing from server configuration.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var metrics = new BenchmarkMetrics();
            
            // We inject the allowed cities list directly into Gemini's context instruction block
            var systemPrompt = "Analyze the passport image. Verify if the barcode/MRZ visual area is clear, intact, and not cut out or altered. " +
                               "Extract the following properties:\n" +
                               "1. Username: extrapolate a clean username from the full name.\n" +
                               "2. KnownAs: First name.\n" +
                               "3. DateOfBirth: format strictly as YYYY-MM-DD.\n" +
                               "4. Country: Return ONLY the 2-letter ISO country code in uppercase, e.g., 'LB' for Lebanon.\n" +
                               "5. Gender: lowercase male or female.\n" +
                               "6. City: Extract the city, but you MUST match it semantically to the closest string match inside this provided JSON array of valid dropdown options. " +
                               $"For example, translate variations like 'Beyrouth' to 'Beirut' if 'Beirut' is in the array. Allowed Cities List: {validCitiesJson}\n\n" +
                               "Return ONLY a raw JSON block matching these exact properties with no markdown styling:\n" +
                               "{\"Username\":\"\",\"KnownAs\":\"\",\"DateOfBirth\":\"\",\"City\":\"\",\"Country\":\"\",\"Gender\":\"\",\"IsBarcodeValid\":true/false}";

            var stopwatch = Stopwatch.StartNew();
            var geminiResponse = await CallGemini(base64Image, systemPrompt, geminiKey);
            stopwatch.Stop();
            
            metrics.GeminiExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            if (string.IsNullOrEmpty(geminiResponse)) 
                return BadRequest("Gemini failed to analyze or extract data from the passport image.");

            try
            {
                if (geminiResponse.Contains("```json"))
                {
                    geminiResponse = geminiResponse.Split("```json")[1].Split("```")[0].Trim();
                }
                else if (geminiResponse.Contains("```"))
                {
                    geminiResponse = geminiResponse.Split("```")[1].Split("```")[0].Trim();
                }

                var parsedData = JsonSerializer.Deserialize<PassportDataDto>(geminiResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                parsedData.Metrics = metrics;

                return Ok(parsedData);
            }
            catch (Exception)
            {
                return BadRequest("AI response format was invalid. Please try a clearer picture.");
            }
        }

        private async Task<string> CallGemini(string base64Image, string prompt, string apiKey)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var requestBody = new
            {
                contents = new[] {
                    new { parts = new object[] {
                        new { text = prompt },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }}
                }
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return "";
                
                var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return jsonDoc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            }
            catch { return ""; }
        }
    }
}