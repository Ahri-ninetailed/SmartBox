namespace Domain.Rules
{
    /// <summary>
    /// Значения слов храним в нижнем регистре без пробелов по краям. По этому же правилу приводим ввод для поиска и парсинга
    /// </summary>
    public static class WordValues
    {
        public static string Normalize(string value)
        {
            return value.Trim().ToLower();
        }

        public static string[] Normalize(IEnumerable<string> values)
        {
            return values.Select(Normalize).ToArray();
        }
    }
}
