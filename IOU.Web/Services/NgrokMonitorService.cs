namespace IOU.Web.Services
{
    public class NgrokMonitorService :BackgroundService
    {
        private readonly NgrokService _ngrok;
        private readonly ILogger<NgrokMonitorService> _logger;

        public NgrokMonitorService(NgrokService ngrok, ILogger<NgrokMonitorService> logger)
        {
            _ngrok = ngrok;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _ngrok.RefreshUrl();
                _logger.LogInformation($"Current Ngrok URL: {_ngrok.PublicUrl}");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Refresh every 5 mins
            }
        }
    }
}
