namespace BlazorApp1.HttpServices
{
    public abstract class HttpClientBase
    {
        public class ResilienceConfig
        {
            public bool Enabled { get; set; }

            public int MaxRetryAttempts { get; set; } = 3;

            public int SamplingDuration { get; set; } = 30000;

            public double FailureRatio { get; set; } = 0.5;

            public int MinimumThroughput { get; set; } = 100;

            public int BreakDuration { get; set; } = 60000;

            public int TimeoutConnect { get; set; } = 3000;
        }

        public string BaseUrl { get; set; } = string.Empty;

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public ResilienceConfig Resilience { get; set; } = new ResilienceConfig();

    }
}
