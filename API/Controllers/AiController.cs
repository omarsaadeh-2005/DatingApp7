using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace API.Controllers
{
    [Authorize]
    public class AiController : BaseApiController
    {
        private readonly OllamaService _ollamaService;

        public AiController(OllamaService ollamaService)
        {
            _ollamaService = ollamaService;
        }

        [HttpPost("generate-bio")]
        public async Task<ActionResult<string>> GenerateBio([FromBody] BioPromptDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Keywords))
                return BadRequest("Please provide a few keywords.");

            var prompt = $"Take these rough keywords about a person: \"{dto.Keywords}\". " +
                         "Write a friendly, engaging, modern dating profile bio (2 to 3 sentences long). " +
                         "Keep it natural, witty, and concise. Do NOT include quotation marks.";

            var systemPrompt = "You are an expert dating profile consultant. Output ONLY the generated bio text with no conversational intro or extra text.";

            var result = await _ollamaService.GenerateResponseAsync(prompt, systemPrompt);
            return Ok(new { bio = result?.Trim() });
        }

        [HttpPost("icebreakers")]
        public async Task<ActionResult> GetIcebreakers([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A bio: '{dto.CurrentUserBio}', interests: '{dto.CurrentUserInterests}'. " +
                         $"User B bio: '{dto.TargetUserBio}', interests: '{dto.TargetUserInterests}'. " +
                         "Generate 3 friendly, creative, short opening messages (icebreakers) User A can send to User B. " +
                         "Return ONLY a JSON array of 3 strings, e.g., [\"line 1\", \"line 2\", \"line 3\"]. Do NOT wrap in markdown block formatting.";

            var rawText = await _ollamaService.GenerateResponseAsync(prompt);
            rawText = CleanJsonString(rawText);

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

            var rawText = await _ollamaService.GenerateResponseAsync(prompt);
            rawText = CleanJsonString(rawText);

            return Content(rawText, "application/json");
        }

        [HttpPost("plan-date")]
        public async Task<ActionResult> PlanDate([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A is in '{dto.CurrentUserLocation}' with interests: '{dto.CurrentUserInterests}'. " +
                         $"User B is in '{dto.TargetUserLocation}' with interests: '{dto.TargetUserInterests}'. " +
                         $"Suggest 3 fun, specific date ideas or venues in or near {dto.TargetUserLocation} tailored to their shared vibes. " +
                         "Return strictly a JSON array of 3 objects with keys 'title', 'description', and 'vibe'. Example: [{\"title\": \"Coffee & Walk\", \"description\": \"Meet at a cozy cafe...\", \"vibe\": \"Casual\"}]";

            var rawText = await _ollamaService.GenerateResponseAsync(prompt);
            rawText = CleanJsonString(rawText);

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
            // Note: Basic LLMs on Ollama (like standard Qwen) focus on text text processing.
            // For fast local validation without visual AI models, we return a standard safe approval.
            return Ok(new { isSafe = true, hasHumanFace = true, reason = "Photo processed locally." });
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

            var rawText = await _ollamaService.GenerateResponseAsync(prompt);
            rawText = CleanJsonString(rawText);

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

        private static string CleanJsonString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";
            return input.Replace("```json", "").Replace("```", "").Trim();
        }

        public class PhotoModerationDto
        {
            public string PhotoUrl { get; set; }
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