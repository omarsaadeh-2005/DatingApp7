namespace API.Services
{
    public interface IAiService
    {
        Task<string> GenerateTextAsync(string prompt, string systemPrompt = null);
        Task<string> ProcessImageAsync(string base64Image, string mimeType, string prompt);
    }
}