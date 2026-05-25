namespace Viagem.Services;

public class TravelStatsBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TravelStatsBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately on startup, then every hour
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var calculator = scope.ServiceProvider.GetRequiredService<TravelStatsCalculator>();
                await calculator.RecalculateAllAsync(stoppingToken);
                logger.LogInformation("Travel stats recalculated at {Time}", DateTime.UtcNow);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error recalculating travel stats");
            }

            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
