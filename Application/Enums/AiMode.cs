namespace Application.Enums
{
    /// <summary>
    /// Режим обработки слов через AI
    /// </summary>
    public enum AiMode
    {
        /// <summary>
        /// AI обработка выключена
        /// </summary>
        Disabled,

        /// <summary>
        /// AI для всех слов (input + Cambridge)
        /// </summary>
        Full,

        /// <summary>
        /// AI только для input слов
        /// </summary>
        InputOnly,

        /// <summary>
        /// Только AI для input слов, без запросов к Cambridge
        /// </summary>
        Exclusive
    }
}
