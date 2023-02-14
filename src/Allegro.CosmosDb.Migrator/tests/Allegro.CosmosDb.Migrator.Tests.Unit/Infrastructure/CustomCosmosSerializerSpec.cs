using System.IO;
using System.Linq;
using Allegro.CosmosDb.Migrator.Infrastructure.CosmosDb.ChangeFeed;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Infrastructure
{
    public class CustomCosmosSerializerSpec
    {
        [Fact]
        public void Able_to_deserialize_json_stream_to_array_of_strings()
        {
            var inputString = "[{\"UserToken\":\"7b2eb9d23dec4f269abcd4de56b19eab\",\"ProcessId\":\"fke3kh4dq221x0150nl7u5ist\",\"EventName\":\"Onboarding.AgreementApproval\",\"Timestamp\":\"2020-08-21T01:40:26.5474777+00:00\",\"Data\":{\"AgreementId\":\"fkc6fl70e000iay4eobswem8s\",\"AgreementType\":\"R01\"},\"id\":\"ad621f6e-33a6-4757-9fca-51989e0569f7\",\"_rid\":\"EiU1AKOMUXEpggUAAAAAAA==\",\"_self\":\"dbs\\/EiU1AA==\\/colls\\/EiU1AKOMUXE=\\/docs\\/EiU1AKOMUXEpggUAAAAAAA==\\/\",\"_etag\":\"\\\"0500d4f8-0000-0d00-0000-5f3f260a0000\\\"\",\"_attachments\":\"attachments\\/\",\"_ts\":1597974026,\"_lsn\":2378746},{\"UserToken\":\"6cb26425807d41ce92deaafd4720e783\",\"ProcessId\":\"fke3kh4e1222i01506k8zs5oa\",\"EventName\":\"Onboarding.AgreementApproval\",\"Timestamp\":\"2020-08-21T01:40:26.5328681+00:00\",\"Data\":{\"AgreementId\":\"fkc3fssbz000yfh4ewf54vb6x\",\"AgreementType\":\"CRIF01\"},\"id\":\"fb1c084c-eeb5-4e65-83da-172bae452191\",\"_rid\":\"EiU1AKOMUXEqggUAAAAAAA==\",\"_self\":\"dbs\\/EiU1AA==\\/colls\\/EiU1AKOMUXE=\\/docs\\/EiU1AKOMUXEqggUAAAAAAA==\\/\",\"_etag\":\"\\\"0500d5f8-0000-0d00-0000-5f3f260a0000\\\"\",\"_attachments\":\"attachments\\/\",\"_ts\":1597974026,\"_lsn\":2378747}]";
            using var stream = GenerateStreamFromString(inputString);

            var serializer = new CustomSerializer();

            var docs = serializer.FromStream<StreamDocumentWrapper[]>(stream);

            docs.Length.ShouldBe(2);
            docs.First().Id.ShouldBe("ad621f6e-33a6-4757-9fca-51989e0569f7");
        }

        [Fact]
        public void Able_to_deserialize_json_stream_to_one_element_with_json_string()
        {
            var inputString = "{\"UserToken\":\"7b2eb9d23dec4f269abcd4de56b19eab\",\"ProcessId\":\"fke3kh4dq221x0150nl7u5ist\",\"EventName\":\"Onboarding.AgreementApproval\",\"Timestamp\":\"2020-08-21T01:40:26.5474777+00:00\",\"Data\":{\"AgreementId\":\"fkc6fl70e000iay4eobswem8s\",\"AgreementType\":\"R01\"},\"id\":\"ad621f6e-33a6-4757-9fca-51989e0569f7\",\"_rid\":\"EiU1AKOMUXEpggUAAAAAAA==\",\"_self\":\"dbs\\/EiU1AA==\\/colls\\/EiU1AKOMUXE=\\/docs\\/EiU1AKOMUXEpggUAAAAAAA==\\/\",\"_etag\":\"\\\"0500d4f8-0000-0d00-0000-5f3f260a0000\\\"\",\"_attachments\":\"attachments\\/\",\"_ts\":1597974026,\"_lsn\":2378746}";
            using var stream = GenerateStreamFromString(inputString);

            var serializer = new CustomSerializer();

            var docs = serializer.FromStream<StreamDocumentWrapper>(stream);

            docs.Id.ShouldBe("ad621f6e-33a6-4757-9fca-51989e0569f7");

        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}