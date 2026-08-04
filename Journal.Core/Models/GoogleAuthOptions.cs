namespace Journal.Models
{
    public class GoogleAuthOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int LoopbackPort { get; set; }
        public string LoopbackHost { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
    }
}
