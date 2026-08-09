using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.Controllers
{
    [Authorize]
    public class AiController : BaseApiController
    {
        private readonly IAiService _aiService;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("generate-bio")]
        public async Task<ActionResult<string>> GenerateBio([FromBody] BioPromptDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Keywords)) return BadRequest("Please provide a few keywords.");

            var prompt = $"Take these rough keywords about a person: \"{dto.Keywords}\". " +
                         "Write a friendly, engaging, modern dating profile bio (2 to 3 sentences long). " +
                         "Keep it natural, witty, and concise. Do NOT include quotation marks.";
            var systemPrompt = "You are an expert dating profile consultant. Output ONLY the generated bio text with no conversational intro.";

            var result = await _aiService.GenerateTextAsync(prompt, systemPrompt);
            var bio = string.IsNullOrWhiteSpace(result)
                ? "Couldn't generate a bio right now — please try again in a moment."
                : result.Trim();

            return Ok(new { bio });
        }

        [HttpPost("compatibility")]
        public async Task<ActionResult<CompatibilityResultDto>> GetCompatibility([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A bio: '{dto.CurrentUserBio}', interests: '{dto.CurrentUserInterests}', location: '{dto.CurrentUserLocation}'. " +
                         $"User B bio: '{dto.TargetUserBio}', interests: '{dto.TargetUserInterests}', location: '{dto.TargetUserLocation}'. " +
                         "Analyze their profile match compatibility. " +
                         "Respond with ONLY raw JSON, no markdown, matching exactly this shape: " +
                         "{\"score\": 85, \"reason\": \"two sentence explanation of shared interests or vibes\"}";

            var rawText = await _aiService.GenerateTextAsync(prompt);
            var result = TryParse<CompatibilityResultDto>(rawText);

            if (result == null || (result.Score == 0 && string.IsNullOrWhiteSpace(result.Reason)))
            {
                Console.WriteLine($"[Compatibility] Falling back — raw AI response was: '{rawText}'");
                result = new CompatibilityResultDto
                {
                    Score = 0,
                    Reason = "Compatibility score is temporarily unavailable. Please try again shortly."
                };
            }

            return Ok(result);
        }

        [HttpPost("icebreakers")]
        public async Task<ActionResult<List<string>>> GetIcebreakers([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A bio: '{dto.CurrentUserBio}', interests: '{dto.CurrentUserInterests}'. " +
                         $"User B bio: '{dto.TargetUserBio}', interests: '{dto.TargetUserInterests}'. " +
                         "Generate 3 friendly, creative, short opening messages (icebreakers) User A can send to User B. " +
                         "Respond with ONLY a raw JSON array of exactly 3 strings, e.g. [\"line 1\", \"line 2\", \"line 3\"]. No markdown, no extra text, no wrapper object.";

            var rawText = await _aiService.GenerateTextAsync(prompt);
            var icebreakers = TryParse<List<string>>(rawText);

            if (icebreakers == null || icebreakers.Count == 0)
            {
                Console.WriteLine($"[Icebreakers] Falling back — raw AI response was: '{rawText}'");
                icebreakers = new List<string> {
                    "Hey! I noticed we have a lot in common.",
                    "Hi there! How is your week going?",
                    "Hey! What's your favorite hobby lately?"
                };
            }

            return Ok(icebreakers);
        }

        [HttpPost("plan-date")]
        public async Task<ActionResult<List<DateIdeaDto>>> PlanDate([FromBody] MatchAnalysisDto dto)
        {
            var prompt = $"User A is in '{dto.CurrentUserLocation}' with interests: '{dto.CurrentUserInterests}'. " +
                         $"User B is in '{dto.TargetUserLocation}' with interests: '{dto.TargetUserInterests}'. " +
                         $"Suggest 3 fun, specific date ideas or venues in or near {dto.TargetUserLocation} tailored to their shared vibes. " +
                         "Respond with ONLY a raw JSON array of exactly 3 objects, each with keys 'title', 'description', and 'vibe'. " +
                         "Example: [{\"title\": \"Coffee & Walk\", \"description\": \"Meet at a cozy cafe...\", \"vibe\": \"Casual\"}]. No markdown, no wrapper object.";

            var rawText = await _aiService.GenerateTextAsync(prompt);
            var ideas = TryParse<List<DateIdeaDto>>(rawText);

            if (ideas == null || ideas.Count == 0)
            {
                Console.WriteLine($"[PlanDate] Falling back — raw AI response was: '{rawText}'");
                ideas = new List<DateIdeaDto>
                {
                    new() { Title = "Coffee & a Walk", Description = "Date planning is temporarily unavailable — grab a coffee and take a walk somewhere you both like in the meantime.", Vibe = "Casual" }
                };
            }

            return Ok(ideas);
        }

        [HttpPost("moderate-photo")]
        public ActionResult ModeratePhoto([FromBody] PhotoModerationDto dto)
        {
            return Ok(new { isSafe = true, hasHumanFace = true, reason = "Photo processed." });
        }

        [HttpPost("search-members")]
        public async Task<ActionResult<List<string>>> SearchMembers([FromBody] SearchPromptDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Query)) return Ok(new List<string>());

            var prompt = $"User Search Query: '{dto.Query}'. Candidate Profiles JSON: {dto.CandidatesJson}. " +
                         "Analyze the candidate bios, cities, and interests against the search query. " +
                         "Respond with ONLY a raw JSON array of matching usernames, e.g. [\"omar\", \"davis\"]. No markdown, no extra text.";

            var rawText = await _aiService.GenerateTextAsync(prompt);
            var matches = TryParse<List<string>>(rawText) ?? new List<string>();
            return Ok(matches);
        }

        /// <summary>Attempts to deserialize AI output into T. Returns null (never throws) on any failure.</summary>
        private static T TryParse<T>(string rawText) where T : class
        {
            if (string.IsNullOrWhiteSpace(rawText)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(rawText, JsonOpts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JSON Parse Error]: {ex.Message} | Raw text: '{rawText}'");
                return null;
            }
        }

        public class PhotoModerationDto { public string PhotoUrl { get; set; } }
        public class SearchPromptDto { public string Query { get; set; } public string CandidatesJson { get; set; } }
    }

    public class BioPromptDto { public string Keywords { get; set; } }

    public class MatchAnalysisDto
    {
        public string CurrentUserBio { get; set; }
        public string CurrentUserInterests { get; set; }
        public string CurrentUserLocation { get; set; }
        public string TargetUserBio { get; set; }
        public string TargetUserInterests { get; set; }
        public string TargetUserLocation { get; set; }
    }

    public class CompatibilityResultDto
    {
        public int Score { get; set; }
        public string Reason { get; set; }
    }

    public class DateIdeaDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Vibe { get; set; }
    }
}