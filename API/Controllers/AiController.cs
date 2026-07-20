using API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace API.Controllers
{
    [Authorize]
    public class AiController : BaseApiController
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AiController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        [HttpPost("generate-bio")]
        public async Task<ActionResult<string>> GenerateBio([FromBody] BioPromptDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Keywords))
                return BadRequest("Please provide a few keywords.");

            var prompt = $"Take these rough keywords about a person: \"{dto.Keywords}\". " +
                         "Write a friendly, engaging, modern dating profile bio (2 to 3 sentences long). " +
                         "Keep it natural, witty, and concise. Do NOT include quotation marks.";

            var result = await CallGeminiAsync(prompt);
            return Ok(new { bio = result?.Trim() });
        }

        [HttpPost("icebreakers")]
        public async Task<ActionResult> GetIcebreakers([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A bio: '{dto.CurrentUserBio}', interests: '{dto.CurrentUserInterests}'. " +
                         $"User B bio: '{dto.TargetUserBio}', interests: '{dto.TargetUserInterests}'. " +
                         "Generate 3 friendly, creative, short opening messages (icebreakers) User A can send to User B. " +
                         "Return ONLY a JSON array of 3 strings, e.g., [\"line 1\", \"line 2\", \"line 3\"]. No markdown or extra text.";

            var rawText = await CallGeminiAsync(prompt);
            rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                var icebreakers = JsonSerializer.Deserialize<List<string>>(rawText);
                return Ok(icebreakers);
            }
            catch
            {
                return Ok(new[] { "Hey! I noticed we have a lot in common.", "Hi there! How is your week going?", "Hey! What's your favorite hobby lately?" });
            }
        }

        [HttpPost("compatibility")]
        public async Task<ActionResult> GetCompatibility([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A bio: '{dto.CurrentUserBio}', interests: '{dto.CurrentUserInterests}', location: '{dto.CurrentUserLocation}'. " +
                         $"User B bio: '{dto.TargetUserBio}', interests: '{dto.TargetUserInterests}', location: '{dto.TargetUserLocation}'. " +
                         "Analyze their profile match compatibility. " +
                         "Return JSON strictly in this format: { \"score\": 85, \"reason\": \"2 sentence explanation of shared interests/vibes.\" }";

            var rawText = await CallGeminiAsync(prompt);
            rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

            return Content(rawText, "application/json");
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var geminiKey = _config["Gemini:ApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) return string.Empty;

            var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return jsonDoc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }

        [HttpPost("plan-date")]
public async Task<ActionResult> PlanDate([FromBody] MatchAnalysisDto dto)
{
    var prompt = $"User A is in '{dto.CurrentUserLocation}' with interests: '{dto.CurrentUserInterests}'. " +
                 $"User B is in '{dto.TargetUserLocation}' with interests: '{dto.TargetUserInterests}'. " +
                 $"Suggest 3 fun, specific date ideas or venues in or near {dto.TargetUserLocation} tailored to their shared vibes. " +
                 "Return strictly a JSON array of 3 objects with keys 'title', 'description', and 'vibe'. Example: [{\"title\": \"Coffee & Walk\", \"description\": \"Meet at a cozy cafe...\", \"vibe\": \"Casual\"}]";

    var rawText = await CallGeminiAsync(prompt);
    rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

    try
    {
        return Content(rawText, "application/json");
    }
    catch
    {
        return BadRequest("Failed to generate date ideas.");
    }
}

[HttpPost("moderate-photo")]
public async Task<ActionResult> ModeratePhoto([FromBody] PhotoModerationDto dto)
{
    var geminiKey = _config["Gemini:ApiKey"];
    // Using gemini-2.5-flash which natively supports image input parts
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiKey}";

    // If the image is a public URL (e.g., Cloudinary), pass it directly or load its base64 bytes
    if (string.IsNullOrWhiteSpace(dto.PhotoUrl)) 
        return BadRequest("Invalid photo URL.");

    byte[] imageBytes;
    string mimeType = "image/jpeg";

    try
    {
        // Fetch image bytes (works for both Cloudinary URLs and local web root paths)
        if (dto.PhotoUrl.StartsWith("http"))
        {
            imageBytes = await _httpClient.GetByteArrayAsync(dto.PhotoUrl);
        }
        else
        {
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", dto.PhotoUrl.TrimStart('/'));
            if (!System.IO.File.Exists(localPath))
            {
                return Ok(new { isSafe = false, hasHumanFace = false, reason = "Photo path not accessible for AI review." });
            }
            imageBytes = await System.IO.File.ReadAllBytesAsync(localPath);
        }

        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Analyze this image for a dating profile. Is it safe/appropriate, and does it contain a real human face/person? Return strictly JSON format: { \"isSafe\": true/false, \"hasHumanFace\": true/false, \"reason\": \"Short 1-sentence reason.\" }" },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64Image
                            }
                        }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return Ok(new { isSafe = true, hasHumanFace = true, reason = "AI scan skipped." });

        var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rawText = jsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        rawText = rawText.Replace("```json", "").Replace("```", "").Trim();
        return Content(rawText, "application/json");
    }
    catch
    {
        return Ok(new { isSafe = true, hasHumanFace = false, reason = "Photo uploaded without AI verification." });
    }
}

public class PhotoModerationDto
{
    public string PhotoUrl { get; set; }
}

        [HttpPost("search-members")]
public async Task<ActionResult<List<string>>> SearchMembers([FromBody] SearchPromptDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Query))
        return Ok(new List<string>());

    var prompt = $"User Search Query: '{dto.Query}'. " +
                 $"Candidate Profiles JSON: {dto.CandidatesJson}. " +
                 "Analyze the candidate bios, cities, and interests against the search query. " +
                 "Return ONLY a JSON array of usernames that match the user's criteria. " +
                 "Example output format: [\"omar\", \"davis\"]. Do NOT include markdown blocks or extra text.";

    var rawText = await CallGeminiAsync(prompt);
    
    // Strip markdown formatting if Gemini includes it
    rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

    try
    {
        var matchedUsernames = JsonSerializer.Deserialize<List<string>>(rawText);
        return Ok(matchedUsernames);
    }
    catch
    {
        return Ok(new List<string>());
    }
}

public class SearchPromptDto
{
    public string Query { get; set; }
    public string CandidatesJson { get; set; }
}
    }

    

    public class BioPromptDto
    {
        public string Keywords { get; set; }
    }

    public class MatchAnalysisDto
    {
        public string CurrentUserBio { get; set; }
        public string CurrentUserInterests { get; set; }
        public string CurrentUserLocation { get; set; }
        public string TargetUserBio { get; set; }
        public string TargetUserInterests { get; set; }
        public string TargetUserLocation { get; set; }
    }
}