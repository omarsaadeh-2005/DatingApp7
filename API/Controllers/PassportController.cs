using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    public class PassportController : BaseApiController
    {
        private readonly IAiService _aiService;

        public PassportController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("process-passport")]
        public async Task<ActionResult<PassportDataDto>> ProcessPassport(IFormFile file, [FromForm] string? validCitiesJson = null)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            Console.WriteLine($"[Passport Processing]: Starting upload for file '{file.FileName}'...");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var metrics = new BenchmarkMetrics();

            var prompt = $@"Extract passport information accurately from the provided image.

CRITICAL JSON SCHEMAS AND INSTRUCTIONS:
1. DateOfBirth: Locate date of birth. Return strictly formatted as YYYY-MM-DD.
2. City: Read the city/place of birth. Select closest match from allowed list: {validCitiesJson ?? "[]"}.
3. Country: Return ONLY the 2-letter uppercase ISO country code (e.g. 'LB', 'FR', 'US').
4. KnownAs: Given/First name.
5. Username: Clean lowercase username without spaces.
6. Gender: Strictly 'male' or 'female'.
7. IsBarcodeValid: true if barcode or MRZ region is clear.

Return ONLY a raw JSON object matching these exact property names:
{{
  ""Username"": """",
  ""KnownAs"": """",
  ""DateOfBirth"": """",
  ""City"": """",
  ""Country"": """",
  ""Gender"": """",
  ""IsBarcodeValid"": true
}}";

            var stopwatch = Stopwatch.StartNew();
            var aiResponse = await _aiService.ProcessImageAsync(base64Image, file.ContentType ?? "image/jpeg", prompt);
            stopwatch.Stop();

            metrics.GeminiExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"[Passport Processing]: Raw AI Response:\n{aiResponse}");

            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                Console.WriteLine("[Passport Error]: AI Service returned an empty response.");
                return BadRequest("AI failed to analyze image. Please check backend logs/settings.");
            }

            try
            {
                var cleanedJson = ExtractJsonBlock(aiResponse);

                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var parsedData = JsonSerializer.Deserialize<PassportDataDto>(cleanedJson, options) 
                                 ?? new PassportDataDto();

                // Post-Processing Safeguards
                if (!string.IsNullOrWhiteSpace(parsedData.KnownAs))
                {
                    var firstName = parsedData.KnownAs.Trim().Split(' ')[0];
                    parsedData.KnownAs = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(parsedData.DateOfBirth))
                {
                    parsedData.DateOfBirth = NormalizeDateToIso(parsedData.DateOfBirth);
                }

                if (!string.IsNullOrEmpty(parsedData.City) && !string.IsNullOrEmpty(validCitiesJson))
                {
                    parsedData.City = NormalizeCity(parsedData.City, validCitiesJson);
                }

                parsedData.Metrics = metrics;
                return Ok(parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Passport Exception]: {ex.Message}");
                return BadRequest($"Failed to parse AI output: {ex.Message}");
            }
        }

        private static string ExtractJsonBlock(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

            var startIndex = input.IndexOf('{');
            var endIndex = input.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return input.Substring(startIndex, endIndex - startIndex + 1);
            }

            return input.Trim();
        }

        private static string NormalizeDateToIso(string rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate)) return string.Empty;
            rawDate = rawDate.Trim();

            if (DateTime.TryParse(rawDate, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }

            string[] formats = {
                "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy", 
                "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy", "yyyyMMdd", "yyMMdd"
            };

            if (DateTime.TryParseExact(rawDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                return parsedDate.ToString("yyyy-MM-dd");
            }

            return rawDate;
        }

        private static string NormalizeCity(string extractedCity, string validCitiesJson)
        {
            try
            {
                var allowedCities = JsonSerializer.Deserialize<List<string>>(validCitiesJson);
                if (allowedCities == null || !allowedCities.Any()) return extractedCity;

                var match = allowedCities.FirstOrDefault(c => c.Equals(extractedCity, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;

                if (extractedCity.Contains("Beyrouth", StringComparison.OrdinalIgnoreCase) || 
                    extractedCity.Contains("Bayrut", StringComparison.OrdinalIgnoreCase))
                {
                    match = allowedCities.FirstOrDefault(c => c.Equals("Beirut", StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }

                match = allowedCities.FirstOrDefault(c => 
                    extractedCity.Contains(c, StringComparison.OrdinalIgnoreCase) || 
                    c.Contains(extractedCity, StringComparison.OrdinalIgnoreCase));

                return match ?? extractedCity;
            }
            catch
            {
                return extractedCity;
            }
        }
    }
}