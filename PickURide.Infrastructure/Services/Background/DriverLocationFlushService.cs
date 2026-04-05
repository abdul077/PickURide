using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PickURide.Application.Interfaces.Services;

namespace PickURide.Infrastructure.Services.Background
{
    public class DriverLocationFlushService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DriverLocationFlushService> _logger;

        public DriverLocationFlushService(IServiceProvider serviceProvider, ILogger<DriverLocationFlushService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DriverLocationFlushService started at {time}", DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IDriverLocationService>();
                    await service.PersistCachedLocationsToDatabaseAsync();
                    _logger.LogInformation("Flushed live driver locations to DB.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while flushing driver locations to DB");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
