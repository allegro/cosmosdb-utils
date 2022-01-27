using System;
using System.Threading;
using System.Threading.Tasks;
using AllegroPay.CosmosDb.BatchUtilities;
using FluentAssertions;
using Xunit;

namespace AllegroPay.CosmosDb.Tests.Unit
{
    public class RateLimiterTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(100)]
        public async Task ShouldNotExceedMaxRate(int maxRate)
        {
            // arrange
            var counter = 0;
            var rateLimiter = new RateLimiter(maxRate, TimeSpan.FromHours(1));
            var func = new Func<Task>(() =>
            {
                Interlocked.Increment(ref counter);
                return Task.CompletedTask;
            });

            // act
            var lastTask = Task.CompletedTask;
            for (var i = 0; i < maxRate + 1; i++)
            {
                lastTask = rateLimiter.ExecuteWeighted(func, weight: 1);
            }

            // assert
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            counter.Should().Be(maxRate);
            lastTask.Should().Match((Task x) => !x.IsCompleted);
        }

        [Fact]
        public async Task ShouldNotExceedMaxRateWithDoubles()
        {
            // arrange
            var counter = 0;
            var rateLimiter = new RateLimiter(12.5, TimeSpan.FromHours(1));
            var func = new Func<Task>(() =>
            {
                Interlocked.Increment(ref counter);
                return Task.CompletedTask;
            });

            // act
            Task lastTask;
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1.5);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 7.5);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 2.98);

            // assert
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            counter.Should().Be(3);
            lastTask.Should().Match((Task x) => !x.IsCompleted);
        }

        [Fact]
        public async Task ShouldResumeWithConfiguredInterval()
        {
            // arrange
            var counter = 0;
            var rateLimiter = new RateLimiter(12.5, TimeSpan.FromSeconds(2));
            var func = new Func<Task>(() =>
            {
                Interlocked.Increment(ref counter);
                return Task.CompletedTask;
            });

            // act
            Task lastTask;
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1.5);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 7.5);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 2.98);

            // assert
            await Task.Delay(TimeSpan.FromSeconds(1));
            counter.Should().Be(3);
            lastTask.Should().Match((Task x) => !x.IsCompleted);

            // delay into - next interval
            await Task.Delay(TimeSpan.FromSeconds(1));

            // act after delay
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 1.5);
            lastTask = rateLimiter.ExecuteWeighted(func, weight: 6.5);

            // assert after delay
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            counter.Should().Be(7);
            lastTask.Should().Match((Task x) => x.IsCompleted);
        }

        [Fact]
        public async Task ShouldNotExceedMaxRateWithEstimatedWeight()
        {
            // arrange
            var counter = 0;
            var rateLimiter = new RateLimiter(12.5, TimeSpan.FromHours(1));
            var funcFactory = new Func<double, Func<Task<double>>>(weight => () =>
            {
                Interlocked.Increment(ref counter);
                return Task.FromResult(weight);
            });

            // act

            // collect weight from first "operation1" execution
            Task lastTask;
            lastTask = rateLimiter.ExecuteWithEstimatedWeight("operation1", funcFactory(5), weight => weight);
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            // run "operation1" operations - previous estimation (5) should be used
            lastTask = rateLimiter.ExecuteWithEstimatedWeight("operation1", funcFactory(3), weight => weight);
            lastTask = rateLimiter.ExecuteWithEstimatedWeight("operation1", funcFactory(5), weight => weight);
            lastTask = rateLimiter.ExecuteWithEstimatedWeight("operation1", funcFactory(1), weight => weight);

            // assert
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            counter.Should().Be(3);
            lastTask.Should().Match((Task x) => !x.IsCompleted);
        }
    }
}