namespace API.DTOs
{
    public class PassportDataDto
    {
        public string Username { get; set; }
        public string KnownAs { get; set; }
        public string DateOfBirth { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Gender { get; set; }
        public bool IsBarcodeValid { get; set; }
        public BenchmarkMetrics Metrics { get; set; }
    }

    public class BenchmarkMetrics
    {
        public double OpenAIExecutionTimeMs { get; set; }
        public double GeminiExecutionTimeMs { get; set; }
        public double CombinedExecutionTimeMs { get; set; }
    }
}