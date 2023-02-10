using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed
{
    internal class CustomSerializer : CosmosSerializer // Copy of CosmosJsonDotNetSerializer from package extended with support of StreamDocumentWrapper
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        private readonly JsonSerializerSettings? _serializerSettings;

        /// <summary>
        /// Create a serializer that uses the JSON.net serializer
        /// </summary>
        /// <remarks>
        /// This is internal to reduce exposure of JSON.net types so
        /// it is easier to convert to System.Text.Json
        /// </remarks>
        public CustomSerializer()
        {
            _serializerSettings = null;
        }

        private static readonly Regex IdRegex = new("\"id\":\\s*\"(?<id>[^,]+)\"", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Convert a Stream to the passed in type.
        /// </summary>
        /// <typeparam name="T">The type of object that should be deserialized</typeparam>
        /// <param name="stream">An open stream that is readable that contains JSON</param>
        /// <returns>The object representing the deserialized stream</returns>
        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                var isStreamDocumentWrapperArray = typeof(StreamDocumentWrapper[]).IsAssignableFrom(typeof(T));
                var isStreamDocumentWrapper = typeof(StreamDocumentWrapper).IsAssignableFrom(typeof(T));

                if (isStreamDocumentWrapperArray || isStreamDocumentWrapper)
                {
                    var result = new List<StreamDocumentWrapper>();

                    using (var sr = new StreamReader(stream))
                    {
                        var serializedArrayOfDocuments = sr.ReadToEnd();
                        stream.Position = 0;

                        SplitSerializedArrayOfDocumentsToCollectionOfDocuments<T>(sr, serializedArrayOfDocuments, result);
                    }

                    return isStreamDocumentWrapperArray
                        ? (T)(object)result.ToArray()
                        : (T)(object)result.SingleOrDefault()!;
                }

                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)stream;
                }

                using (var sr = new StreamReader(stream))
                {
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        var jsonSerializer = GetSerializer();
                        return jsonSerializer.Deserialize<T>(jsonTextReader)!;
                    }
                }
            }
        }

        private static void SplitSerializedArrayOfDocumentsToCollectionOfDocuments<T>(
            StreamReader sr,
            string serializedArrayOfDocuments,
            List<StreamDocumentWrapper> result)
        {
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                var objectTokens = 0;
                var startPosition = -1;

                while (jsonTextReader.Read())
                {
                    if (jsonTextReader.TokenType == JsonToken.StartObject)
                    {
                        if (objectTokens == 0)
                        {
                            startPosition = jsonTextReader.LinePosition;
                        }

                        objectTokens++;
                        continue;
                    }

                    if (jsonTextReader.TokenType == JsonToken.EndObject)
                    {
                        objectTokens--;

                        if (objectTokens == 0)
                        {
                            ExtractObjectFromSerializedArray(serializedArrayOfDocuments, result, jsonTextReader, startPosition);
                        }
                    }
                }
            }
        }

        private static void ExtractObjectFromSerializedArray(
            string serializedArrayOfDocuments,
            List<StreamDocumentWrapper> result,
            JsonTextReader jsonTextReader,
            int startPosition)
        {
            var endPosition = jsonTextReader.LinePosition;

            var objectSerialized = serializedArrayOfDocuments.Substring(
                startPosition - 1,
                endPosition - startPosition + 1);
            var idMatches = IdRegex.Matches(objectSerialized);
            result.Add(
                new StreamDocumentWrapper(
                    objectSerialized,
                    idMatches.Any()
                        ? idMatches.First().Groups["id"].Value
                        : string.Empty));
        }

        /// <summary>
        /// Converts an object to a open readable stream
        /// </summary>
        /// <typeparam name="T">The type of object being serialized</typeparam>
        /// <param name="input">The object to be serialized</param>
        /// <returns>An open readable stream containing the JSON of the serialized object</returns>
        public override Stream ToStream<T>(T input)
        {
            if (input is StreamDocumentWrapper wrapper)
            {
                return GenerateStreamFromString(wrapper.Json);
            }

            var streamPayload = new MemoryStream();
            using (var streamWriter = new StreamWriter(
                streamPayload,
                encoding: CustomSerializer.DefaultEncoding,
                bufferSize: 1024,
                leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    var jsonSerializer = GetSerializer();
                    jsonSerializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            streamPayload.Position = 0;
            return streamPayload;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// JsonSerializer has hit a race conditions with custom settings that cause null reference exception.
        /// To avoid the race condition a new JsonSerializer is created for each call
        /// </summary>
        private JsonSerializer GetSerializer()
        {
            return JsonSerializer.Create(_serializerSettings);
        }
    }
}