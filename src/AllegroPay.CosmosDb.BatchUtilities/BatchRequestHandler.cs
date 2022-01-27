using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AllegroPay.CosmosDb.BatchUtilities
{
    public class BatchRequestHandler : RequestHandler
    {
        private readonly ILogger<BatchRequestHandler> _logger;
        private readonly Dictionary<string, IRateLimiter> _ruLimiters;

        public BatchRequestHandler(
            ILogger<BatchRequestHandler> logger,
            params BatchUtilitiesRegistration[] ruLimiterRegistrations)
        {
            _logger = logger;
            _ruLimiters = ruLimiterRegistrations.ToDictionary(
                x => x.IsDatabaseRegistration ? FormatKey(x.DatabaseName) : FormatKey(x.DatabaseName, x.ContainerName!),
                x => (IRateLimiter)x.RuLimiter);
        }

        public override Task<ResponseMessage> SendAsync(
            RequestMessage request,
            CancellationToken cancellationToken)
        {
            var getRuLimiterResult = TryGetRuLimiterAndOperationName(request);
            if (getRuLimiterResult == null)
            {
                return base.SendAsync(request, cancellationToken);
            }

            var (ruLimiter, operationName) = getRuLimiterResult.Value;
            return ruLimiter.ExecuteWithEstimatedWeight(
                operationName,
                () => base.SendAsync(request, cancellationToken),
                response => response?.Headers.RequestCharge);
        }

        private (IRateLimiter RateLimiter, string OperationName)? TryGetRuLimiterAndOperationName(RequestMessage request)
        {
            IRateLimiter? rateLimiter;
            string operationName;

            try
            {
                var resourceNames = request.ExtractResourceNamesFromRequest();
                if (resourceNames == null)
                {
                    _logger.LogError("Extracting resource names from cosmos request failed!");
                    return null;
                }

                var (databaseName, collectionName) = resourceNames.Value;
                if (!_ruLimiters.TryGetValue(FormatKey(databaseName), out rateLimiter) &&
                    !_ruLimiters.TryGetValue(FormatKey(databaseName, collectionName), out rateLimiter))
                {
                    throw new Exception(
                        $"No RuLimiter registered for database {databaseName}, collection {collectionName}");
                }

                operationName =
                    $"{databaseName}_{collectionName}_{request.Method}_{request.GetPropertyValue("OperationType")}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Getting RU limiter failed!");
                return null;
            }

            return (rateLimiter, operationName);
        }

        private static string FormatKey(string databaseName, string containerName)
        {
            return $"{databaseName.ToLowerInvariant()}-{containerName.ToLowerInvariant()}";
        }

        private static string FormatKey(string databaseName)
        {
            return databaseName.ToLowerInvariant();
        }
    }
}