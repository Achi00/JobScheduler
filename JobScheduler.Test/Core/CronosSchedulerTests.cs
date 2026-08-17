using JobScheduler.Core.Recurring;

namespace JobScheduler.Test.Core
{
    public class CronosSchedulerTests
    {
        private readonly CronosScheduler _scheduler = new();

        [Fact]
        public void GetNextOccurrence_WithHourlyExpression_ReturnsNextHourBoundary()
        {
            var from = new DateTimeOffset(2026, 1, 1, 10, 15, 0, TimeSpan.Zero);
            var next = _scheduler.GetNextOccurrence("0 * * * *", "UTC", from);
            Assert.Equal(new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero), next);
        }
    }
}
