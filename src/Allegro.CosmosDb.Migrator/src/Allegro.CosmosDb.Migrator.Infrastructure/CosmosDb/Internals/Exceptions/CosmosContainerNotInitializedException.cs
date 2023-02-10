using System;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.Internals.Exceptions
{
    internal class CosmosContainerNotInitializedException : Exception
    {
        public CosmosContainerNotInitializedException(string documentTypeKey) : base($"Container for document type {documentTypeKey} was not initialized")
        {
        }
    }
}