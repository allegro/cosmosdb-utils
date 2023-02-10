using System;
using Allegro.CosmosDb.Migrator.Core.Exceptions;

namespace Allegro.CosmosDb.Migrator.Core.Helpers
{
    public static class EnsureExtensions
    {
        public static void EnsureNotEmpty(this string str, string name)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new InvalidArgumentException(name, "Null or empty");
            }
        }

        public static void Ensure(this DateTime dateTime, string name)
        {
        }
    }
}