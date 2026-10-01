namespace Application.Settings
{
    public class AiSettings
    {
        /// <summary>
        /// Значения, у которых примеров меньше, дозаполняются через AI
        /// </summary>
        public int FillMissingExamplesIfBelow { get; }

        /// <summary>
        /// Сколько примеров просить на значение при дозаполнении
        /// </summary>
        public int ExamplesPerMeaningForFilling { get; }

        /// <summary>
        /// Сколько примеров просить на значение для слов, которые целиком ищет AI
        /// </summary>
        public int ExamplesPerMeaningForNewWords { get; }

        public AiSettings(int fillMissingExamplesIfBelow, int examplesPerMeaningForFilling, int examplesPerMeaningForNewWords)
        {
            FillMissingExamplesIfBelow = fillMissingExamplesIfBelow;
            ExamplesPerMeaningForFilling = examplesPerMeaningForFilling;
            ExamplesPerMeaningForNewWords = examplesPerMeaningForNewWords;
        }
    }
}
