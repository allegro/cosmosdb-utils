using System;
using System.ComponentModel;
using System.Linq;
using Allegro.CosmosDb.Migrator.Application.Services;
using Newtonsoft.Json.Linq;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.DocumentClient
{
    internal class DocumentWrapper : IDocument
    {
        private readonly JObject _document;

        public DocumentWrapper(dynamic document)
        {
            _document = document;
        }

        public string Id => GetValueAsString("id");

        public DateTime Timestamp => GetValue<DateTime>("_ts");

        public T? GetValue<T>(string propertyName)
        {
            var value = GetValueAsString(propertyName);
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T?)converter.ConvertFromInvariantString(value);
        }

        public string GetValueAsString(string propertyName)
        {
            var property = _document.Properties().SingleOrDefault(p =>
                p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

            if (property is null)
            {
                return string.Empty;
            }

            return property.Value.ToString();
        }
    }
}