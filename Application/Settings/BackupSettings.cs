namespace Application.Settings
{
    public class BackupSettings
    {
        /// <summary>
        /// Новый бэкап делается, если с последнего прошло больше этого времени
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// Сколько последних бэкапов хранить
        /// </summary>
        public int KeepCount { get; }

        public BackupSettings(TimeSpan interval, int keepCount)
        {
            Interval = interval;
            KeepCount = keepCount;
        }
    }
}
