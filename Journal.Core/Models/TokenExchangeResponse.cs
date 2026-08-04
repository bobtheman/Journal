namespace Journal.Models
{
    // ReSharper disable InconsistentNaming
    public class TokenExchangeResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string? refresh_token { get; set; }
        public int expires_in { get; set; }
    }
    // ReSharper restore InconsistentNaming
}
