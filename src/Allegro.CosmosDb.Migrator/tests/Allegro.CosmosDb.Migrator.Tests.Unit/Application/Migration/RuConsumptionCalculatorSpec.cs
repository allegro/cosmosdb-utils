using Allegro.CosmosDb.Migrator.Application.Migrations.Processor;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Application.Migration
{
    public class RuConsumptionCalculatorSpec
    {
        [Fact]
        public void Max_ru_not_set()
        {
            //given:
            var ruConsumptionCalculator = new RuConsumptionCalculator(maxRu: 0, initialDocumentCountInBatch: 0);
            //than
            ruConsumptionCalculator.CalculateMaxDocumentPerSecond().ShouldBe(0);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1)]
        public void Current_ru_not_set(int initialDocumentCount)
        {
            //given:
            var ruConsumptionCalculator = new RuConsumptionCalculator(maxRu: 1000, initialDocumentCount);
            //than
            ruConsumptionCalculator.CalculateMaxDocumentPerSecond().ShouldBe(initialDocumentCount);
        }

        [Theory]
        [InlineData(10, 10, 10)]
        [InlineData(10, 20, 5)]
        [InlineData(10, 21, 4)]
        [InlineData(10, 19, 5)]
        [InlineData(10, 5, 20)]
        [InlineData(10, 6, 16)]
        [InlineData(10, 4, 25)]
        public void Calculate_max_documents_count_per_second(
            int currentDocumentCount,
            double currentRuConsumption,
            int maxDocumentCount)
        {
            const int maxRu = 10;
            //given:
            var ruConsumptionCalculator = new RuConsumptionCalculator(maxRu, initialDocumentCountInBatch: 10);
            //when:
            ruConsumptionCalculator.SetCurrentRuConsumption(currentDocumentCount, currentRuConsumption);
            var calculatedMaxDocumentCount = ruConsumptionCalculator.CalculateMaxDocumentPerSecond();
            //then:
            calculatedMaxDocumentCount.ShouldBe(maxDocumentCount);
        }
    }
}