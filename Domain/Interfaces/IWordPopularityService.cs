namespace Domain.Interfaces
{
    public interface IWordPopularityService
    {
        /// <summary>
        /// Gets the popularity score for a word or its synonyms from Google Books Ngram Viewer.
        /// </summary>
        /// <param name="words">A single word or array of synonyms for the same word. When multiple synonyms are provided, returns the average popularity across all variations.</param>
        /// <returns>The popularity score as a decimal value, or null if no data is available.</returns>
        Task<decimal?> GetWordPopularityAsync(string[] words);
    }
}
