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

        [Fact]
        public void GetNextOccurrence_WithInvalidCronExpression_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scheduler.GetNextOccurrence("Invalid Cronos", "00", DateTimeOffset.UtcNow));
        }

        [Fact]
        public void GetNextOccurrence_WithInvalidTimeZone_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _scheduler.GetNextOccurrence("0 * * * *", "N/A", DateTimeOffset.UtcNow));
        }

        // passing valid cron expression and zone id
        [Fact]
        public void GetNextOccurrence_WithNonUtcTimeZone_ConvertsCorrectly()
        {
            // "0 9 * * *" = 9am daily, in Asia/Tbilisi (UTC+4, no DST) = 5am UTC
            var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var next = _scheduler.GetNextOccurrence("0 9 * * *", "Asia/Tbilisi", from);

            Assert.Equal(new DateTimeOffset(2026, 1, 1, 5, 0, 0, TimeSpan.Zero), next);
        }

        [Fact]
        public void GetNextOccurrence_AcrossDstTransition_HandlesCorrectly()
        {
            // DST zones Europe/London, America/New_York
            // "from" timestamp right around a known DST boundary date, ~1-2 minute
            var from = new DateTimeOffset(2026, 3, 29, 0, 59, 0, TimeSpan.Zero);

            var next = _scheduler.GetNextOccurrence("* * * * *", "Europe/London", from);

            var expectedUtc = new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero);

            Assert.Equal(expectedUtc, next);
        }
    }
}
