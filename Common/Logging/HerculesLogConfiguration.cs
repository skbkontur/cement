namespace Common.Logging
{
    internal sealed class HerculesLogConfiguration
    {
        public bool Enabled { get; set; }
        public string Stream { get; set; }
        public string ApiKey { get; set; }
        public string ServerUrl { get; set; }
        public string Project { get; set; }
        public string Environment { get; set; }
        public long MaximumMemoryConsumptionInBytes { get; set; }
    }
}