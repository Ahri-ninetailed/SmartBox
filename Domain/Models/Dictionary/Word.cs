using Domain.Rules;

namespace Domain.Models.Dictionary
{
    public class Word
    {
        private const string WordAlreadyLearnedError = "Word '{0}' is already learned";
        private const string WordAlreadyMarkedForStudyError = "Word '{0}' is already marked for study";
        private const string ValuesNullOrEmptyError = "Values array cannot be null, empty, or contain null/whitespace elements.";
        private const string PartOfSpeechUnknownError = "Part of speech cannot be unknown";

        public int Id { get; set; }

        private string[] _values = [];
        public string[] Values
        {
            get => _values;
            set
            {
                ValidateValues(value);

                _values = value;
            }
        }

        public decimal? Popularity { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }
        public bool ShouldStudy { get; set; }
        public bool Learned { get; set; }
        public string Comments { get; set; } = string.Empty;

        public List<Definition> Definitions { get; set; } = [];

        /// <summary>
        /// Новое слово с нормализованными значениями. Слова из БД создаются без нормализации, чтобы их не переписать при сохранении
        /// </summary>
        public static Word CreateNew(string[] values, PartOfSpeech partOfSpeech, string comments)
        {
            // Проверяем до нормализации, иначе пробелы превратятся в пустые строки, а null упадет в Trim
            ValidateValues(values);

            return new Word
            {
                Values = WordValues.Normalize(values),
                PartOfSpeech = partOfSpeech,
                Comments = comments
            };
        }

        public void Learn()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, string.Join(", ", Values)));
            }

            Learned = true;

            ShouldStudy = false;
        }

        public void MarkForStudy()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, string.Join(", ", Values)));
            }

            if (ShouldStudy)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyMarkedForStudyError, string.Join(", ", Values)));
            }

            ShouldStudy = true;
        }

        public void ChangePartOfSpeech(PartOfSpeech newPartOfSpeech)
        {
            if (newPartOfSpeech == PartOfSpeech.Unknown)
            {
                throw new ArgumentException(PartOfSpeechUnknownError, nameof(newPartOfSpeech));
            }

            PartOfSpeech = newPartOfSpeech;
        }

        private static void ValidateValues(string[]? values)
        {
            if (values == null || values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(ValuesNullOrEmptyError, nameof(Values));
            }
        }
    }
}
