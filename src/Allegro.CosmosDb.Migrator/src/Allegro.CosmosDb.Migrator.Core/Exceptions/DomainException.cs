using System;

namespace Allegro.CosmosDb.Migrator.Core.Exceptions
{
    public abstract class DomainException : Exception
    {
        public abstract string Code { get; }

        protected DomainException(string message) : base(message)
        {
        }
    }

    internal class InvalidArgumentException : DomainException
    {
        public override string Code => "invalid_argument";

        public InvalidArgumentException(string argumentName, string reason) : base($"Provided argument is not valid. Name {argumentName}. Reason: {reason} ")
        {
        }
    }
}