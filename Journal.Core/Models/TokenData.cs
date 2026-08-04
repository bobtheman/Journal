namespace Journal.Models
{
    public class TokenData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
