namespace IOU.Web.Config
{
    public class MpesaConfiguration
    {
        public required string ConsumerKey { get; set; }
        public required string ConsumerSecret { get; set; }
        public required string BaseUrl { get; set; }
        public required string LipaNaMpesaOnlinePassKey { get; set; }
        
        public required string BusinessShortCode { get; set; }
        public required string CallbackUrl { get; set; }
        public bool UseNgrok { get; set; } = false;
        public string? NgrokUrl { get; set; }
    }
}
