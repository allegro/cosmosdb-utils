using System;

namespace Allegro.CosmosDb.Migrator.Application
{
    public abstract class AppException : Exception
    {
        public abstract string Code { get; }

        protected AppException(string message) : base(message)
        {
        }
    }
}