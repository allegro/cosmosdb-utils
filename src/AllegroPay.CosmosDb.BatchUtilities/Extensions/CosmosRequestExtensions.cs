using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;

namespace AllegroPay.CosmosDb.BatchUtilities
{
    public static class CosmosRequestUtils
    {
        private const string RequestUriRegexDatabaseGroupName = "database";
        private const string RequestUriRegexCollectionGroupName = "collection";

        private static readonly Regex RequestUriRegex = new(
            @$"^dbs\/(?<{RequestUriRegexDatabaseGroupName}>.*?)\/colls\/(?<{RequestUriRegexCollectionGroupName}>.*?)(\/.*)?$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

        private static readonly ConcurrentDictionary<string, (string, string)?> RequestUriCache = new();

        public static (string DatabaseName, string CollectionName)? ExtractResourceNamesFromRequest(
            this RequestMessage request)
        {
            return RequestUriCache.GetOrAdd(
                request.RequestUri.ToString(),
                key =>
                {
                    var match = RequestUriRegex.Match(key);
                    if (!match.Success)
                    {
                        return null;
                    }

                    return (
                        match.Groups[RequestUriRegexDatabaseGroupName].Value,
                        match.Groups[RequestUriRegexCollectionGroupName].Value);
                });
        }
    }
}