namespace Domain.Models.Backup
{
    public class BackupFile
    {
        public string Path { get; }
        public DateTime CreatedAt { get; }

        public BackupFile(string path, DateTime createdAt)
        {
            Path = path;
            CreatedAt = createdAt;
        }

        public bool IsOlderThan(TimeSpan age, DateTime now)
        {
            return now - CreatedAt >= age;
        }
    }
}
