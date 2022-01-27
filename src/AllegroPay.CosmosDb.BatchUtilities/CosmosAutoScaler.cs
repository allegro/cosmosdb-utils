using System;
using System.Threading;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities.Configuration;
using Microsoft.Extensions.Logging;

#pragma warning disable VSTHRD110
namespace AllegroPay.CosmosDb.BatchUtilities
{
    public interface ICosmosAutoScaler
    {
        void ReportBatchStart();
    }

    public class CosmosAutoScaler : ICosmosAutoScaler
    {
        private readonly ILogger<CosmosAutoScaler> _logger;
        private readonly ICosmosScalableObject _scalableObject;
        private readonly IRateLimiterWithVariableRate _ruLimiter;
        private readonly CosmosBatchUtilitiesConfiguration _configuration;

        private readonly Timer _timer = null!; // just keeping reference to prevent GC collection

        private bool _scaledUp;

        private DateTimeOffset _lastBatchReported = DateTimeOffset.MinValue;

        public CosmosAutoScaler(
            ILogger<CosmosAutoScaler> logger,
            ICosmosScalableObject scalableObject,
            IRateLimiterWithVariableRate ruLimiter,
            CosmosBatchUtilitiesConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scalableObject = scalableObject ?? throw new ArgumentNullException(nameof(scalableObject));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _ruLimiter = ruLimiter;

            if (configuration.AutoScaler?.Enabled == true)
            {
                _timer = new Timer(
                    TimerCallback,
                    null,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1));
            }
        }

        public void ReportBatchStart()
        {
            if (_configuration.AutoScaler?.Enabled != true)
            {
                return;
            }

            lock (_timer)
            {
                _lastBatchReported = DateTimeOffset.UtcNow;
                if (_scaledUp)
                {
                    return;
                }

                _scaledUp = true;
            }

            Task.Run(
                async () =>
                {
                    _logger.LogInformation(
                        "Scaling up to {ProcessingMaxThroughput} ({ProvisioningMode})",
                        _configuration.AutoScaler.ProcessingMaxThroughput,
                        _configuration.AutoScaler.ProvisioningMode);
                    try
                    {
                        await _scalableObject.Scale(_configuration.AutoScaler.ProcessingMaxThroughput, _configuration.AutoScaler.ProvisioningMode);
                        _scaledUp = true;

                        await AdjustMaxRuAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while scaling up.");
                        _scaledUp = false;
                    }
                });
        }

        private void TimerCallback(object? state)
        {
            if (_configuration.AutoScaler?.Enabled != true)
            {
                return;
            }

            DownScaler();
        }

        private void DownScaler()
        {
            async Task ScaleDown()
            {
                _logger.LogInformation(
                    "Scaling down to {IdleMaxThroughput} ({ProvisioningMode})",
                    _configuration.AutoScaler!.IdleMaxThroughput,
                    _configuration.AutoScaler.ProvisioningMode);
                try
                {
                    _scaledUp = false;
                    _ruLimiter.ChangeMaxRate(_configuration.MaxRu);

                    await _scalableObject.Scale(_configuration.AutoScaler.IdleMaxThroughput, _configuration.AutoScaler.ProvisioningMode);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while scaling down.");
                }
            }

            switch (_scaledUp)
            {
                // scaled up & no batch was processed for configured grace period - scale down
                case true when DateTimeOffset.UtcNow - _lastBatchReported > _configuration.AutoScaler!.DownscaleGracePeriod:
                    Task.Run(ScaleDown);
                    return;
                // scaled up & processing batches - check if scaling has ended and adjust Max RU
                case true:
                    Task.Run(AdjustMaxRuAsync);
                    break;
                // should not be scaled up - check to prevent leaving Cosmos scaled up due to previous error
                case false:
                    _ruLimiter.ChangeMaxRate(_configuration.MaxRu);
                    Task.Run(
                        async () =>
                        {
                            if (await CheckIsCosmosScaledUp())
                            {
                                await ScaleDown();
                            }
                        });
                    break;
            }
        }

        private async Task AdjustMaxRuAsync()
        {
            if (_ruLimiter == null)
            {
                return;
            }

            var scaledUp = await CheckIsCosmosScaledUp();

            if (scaledUp && (int)_ruLimiter.MaxRate != (int)_configuration.AutoScaler!.ProcessingMaxRu)
            {
                _logger.LogInformation(
                    "Detected cosmos is scaled up. Changing RuLimiter.MaxRu to {MaxRu}",
                    _configuration.AutoScaler.ProcessingMaxRu);
                _ruLimiter.ChangeMaxRate(_configuration.AutoScaler.ProcessingMaxRu);
            }

            if (!scaledUp && (int)_ruLimiter.MaxRate != (int)_configuration.MaxRu)
            {
                _logger.LogInformation(
                    "Detected cosmos is scaled down. Changing RuLimiter.MaxRu to {MaxRu}",
                    _configuration.MaxRu);
                _ruLimiter.ChangeMaxRate(_configuration.MaxRu);
            }
        }

        private async Task<bool> CheckIsCosmosScaledUp()
        {
            var throughput = await _scalableObject.GetCurrentThroughput();

            if (throughput.IsReplacePending == true)
            {
                return false;
            }

            return _configuration.AutoScaler!.ProvisioningMode switch
            {
                CosmosProvisioningMode.AutoScale
                    when throughput.Resource?.AutoscaleMaxThroughput ==
                         _configuration.AutoScaler.ProcessingMaxThroughput
                    => true,
                CosmosProvisioningMode.Manual
                    when throughput.Resource?.Throughput == _configuration.AutoScaler.ProcessingMaxThroughput
                    => true,
                _ => false
            };
        }
    }
}