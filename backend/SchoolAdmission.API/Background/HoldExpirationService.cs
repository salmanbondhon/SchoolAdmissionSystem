
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Data;
using SchoolAdmission.API.Enums;

namespace SchoolAdmission.API.Background;

public class HoldExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpirationService> _logger;

    public HoldExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Hold expiration background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;

                var expiredCount = await context.AdmissionHolds
                    .Where(h =>
                        h.Status == HoldStatus.Active &&
                        h.ExpiresAt <= now)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            h => h.Status,
                            HoldStatus.Expired),
                        stoppingToken);

                if (expiredCount > 0)
                {
                    _logger.LogInformation(
                        "Expired {Count} admission holds.",
                        expiredCount);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while expiring admission holds.");
            }

            // Check every 5 seconds.
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}
