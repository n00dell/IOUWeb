using IOU.Web.Data;
using IOU.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Services
{
    public class PaymentCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PaymentCleanupService> _logger;
        public PaymentCleanupService(IServiceProvider services, ILogger<PaymentCleanupService> logger)
        {
        _services = services;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment cleanup service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IOUWebContext>();

                    var cutoffTime = DateTime.UtcNow.AddMinutes(-15);
                    var stalePayments = await db.Payments
                        .Where(p => p.Status == PaymentTransactionStatus.Pending &&
                                   p.CreatedAt < cutoffTime)
                        .ToListAsync(stoppingToken);

                    foreach (var payment in stalePayments)
                    {
                        payment.Status = PaymentTransactionStatus.Failed;
                        payment.FailureReason = "Transaction timed out";
                        _logger.LogWarning($"Marked payment {payment.Id} as timed out");
                    }

                    if (stalePayments.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up payments");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
