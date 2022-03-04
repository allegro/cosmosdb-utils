using System;
using System.Reflection;

namespace Allegro.CosmosDb.BatchUtilities.Extensions
{
    internal static class ReflectionHelper
    {
        public static object? GetPropertyValue(this object instance, string propertyName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var instanceType = instance.GetType();
            var propertyInfo = instanceType.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(propertyName),
                    $"No property {propertyName} could be found in {instanceType.FullName}");
            }

            return propertyInfo.GetValue(instance, null);
        }
    }
}