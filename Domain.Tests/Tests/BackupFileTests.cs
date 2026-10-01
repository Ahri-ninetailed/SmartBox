using Domain.Models.Backup;

namespace Domain.Tests.Tests
{
    public class BackupFileTests
    {
        private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0);
        private static readonly TimeSpan Day = TimeSpan.FromHours(24);

        [Fact]
        public void IsOlderThan_CreatedEarlierThanAge_ReturnsTrue()
        {
            Assert.True(new BackupFile("backup.dump", Now.AddHours(-25)).IsOlderThan(Day, Now));
        }

        [Fact]
        public void IsOlderThan_CreatedExactlyAgeAgo_ReturnsTrue()
        {
            Assert.True(new BackupFile("backup.dump", Now.AddHours(-24)).IsOlderThan(Day, Now));
        }

        [Fact]
        public void IsOlderThan_CreatedLaterThanAge_ReturnsFalse()
        {
            Assert.False(new BackupFile("backup.dump", Now.AddHours(-12)).IsOlderThan(Day, Now));
        }
    }
}
