using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals.Exceptions
{
    internal class CosmosTransactionAlreadyBeganException : Exception
    {
        public CosmosTransactionAlreadyBeganException(string repositoryName) : base($"Transaction already began for repository {repositoryName}")
        {
        }
    }

    internal class CosmosTransactionNotInitializedException : Exception
    {
        public CosmosTransactionNotInitializedException(string repositoryName) : base($"Transaction was not initialized for repository {repositoryName}")
        {
        }
    }

    internal class CosmosTransactionCommitException : Exception
    {
        public IReadOnlyCollection<CosmosOperationError> Errors { get; }

        public CosmosTransactionCommitException(string repositoryName, TransactionalBatchResponse response) : base($"Transaction commit failed on {repositoryName} with resultCode {response.StatusCode} and message: {response.ErrorMessage}")
        {
            Errors = response.Where(operation => !operation.IsSuccessStatusCode)
                .Select(operation => new CosmosOperationError(operation.StatusCode, operation?.ToString())).ToImmutableArray();
        }
    }

    internal class CosmosOperationError
    {
        public HttpStatusCode StatusCode { get; }
        public string Message { get; }

        public CosmosOperationError(HttpStatusCode statusCode, string? message)
        {
            StatusCode = statusCode;
            Message = message ?? string.Empty;
        }
    }
}