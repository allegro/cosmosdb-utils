using System;

namespace Allegro.CosmosDb.Migrator.Core.Entities
{
    public readonly struct Option<T> where T : class
    {
#pragma warning disable MA0018
        public static Option<T> Empty => new(default);
#pragma warning restore MA0018

        private readonly T? _value;

        public Option(T? value)
        {
            IsPresent = value is { };
            _value = value;
        }

        public bool IsPresent { get; }

        public bool IsNull => !IsPresent;

        public T Value
        {
            get
            {
                if (IsNull)
                {
                    throw new InvalidOperationException("Value is not present.");
                }

                return _value!;
            }
        }
    }
}