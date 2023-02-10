using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Allegro.CosmosDb.Migrator.Core.Migrations;
using Shouldly;
using Xunit;

namespace Allegro.CosmosDb.Migrator.Tests.Unit.Core.Migrations
{
    public class StatisticsSpec
    {
        [Fact]
        public void When_started_statistics_will_be_created_with_defaults()
        {
            var statistics = CreateStartedStatistics();

            statistics.ShouldNotBeNull();
            statistics.DestinationCount.ShouldBe(default);
            statistics.SourceCount.ShouldBe(default);
            statistics.EndTime.ShouldBe(DateTime.MinValue);
            statistics.Percentage.ShouldBe(double.NaN);
            statistics.Eta.ShouldBe(TimeSpan.Zero);
            statistics.LastUpdate.ShouldBe(DateTime.MinValue);
            statistics.StartTime.ShouldNotBe(DateTime.MinValue);
            statistics.StartTime.ShouldBeLessThan(DateTime.UtcNow);
        }

        [Theory]
        [InlineData(1000, 0, 0)]
        [InlineData(1000, 1000, 1)]
        [InlineData(1000, 500, 0.5)]
        [InlineData(0, 0, double.NaN)]
        [InlineData(0, 1000, double.PositiveInfinity)]
        public void When_update_progress_percentage_should_be_as_expected(long sourceCount, long destinationCount,
            double expectedPercentage)
        {
            var statistics = CreateStartedStatistics();

            statistics.Update(sourceCount, destinationCount);

            statistics.Percentage.ShouldBe(expectedPercentage);
        }

        [Fact]
        public void When_statistics_completed_EndTime_Should_Be_Set()
        {
            var statistics = CreateStartedStatistics();

            statistics.Complete();

            statistics.EndTime.ShouldNotBe(DateTime.MinValue);
            statistics.EndTime.ShouldBeLessThan(DateTime.UtcNow);
        }

        [Fact]
        public async Task When_update_progress_in_pace_per_second_average_should_be_set_corresponding_to_pace()
        {
            var stopWatch = Stopwatch.StartNew();
            var statistics = CreateStartedStatistics();

            await Update_progress_with_around_90_docs_per_sec(statistics);
            stopWatch.Stop();

            var expectedLowestAverage = 20 / stopWatch.ElapsedMilliseconds * 1000;
            statistics.CurrentInsertRatePerSeconds.ShouldBeLessThan(100);
            statistics.CurrentInsertRatePerSeconds.ShouldBeGreaterThan(80);
            statistics.AverageInsertRatePerSeconds.ShouldBeLessThan(statistics.CurrentInsertRatePerSeconds);
            statistics.AverageInsertRatePerSeconds.ShouldBeGreaterThan(expectedLowestAverage);
        }

        [Fact]
        public async Task When_update_progress_eta_should_be_set_corresponding_to_pace()
        {
            var statistics = CreateStartedStatistics();

            await Update_progress_with_around_90_docs_per_sec(statistics);

            statistics.Eta.ShouldBeLessThan(TimeSpan.FromSeconds(13));
            statistics.Eta.ShouldBeGreaterThan(TimeSpan.FromSeconds(9));
        }

        private static async Task Update_progress_with_around_90_docs_per_sec(Statistics statistics)
        {
            var (sourceCount, destinationCount) = (1000, 0);
            var pace = 10;

            statistics.Update(sourceCount, destinationCount);
            destinationCount += pace;
            await Task.Delay(100);
            statistics.Update(sourceCount, destinationCount);
            statistics.Complete();
        }

        private static Statistics CreateStartedStatistics()
        {
            var statistics = Statistics.Create(Guid.NewGuid());
            statistics.Start();
            return statistics;
        }
    }
}