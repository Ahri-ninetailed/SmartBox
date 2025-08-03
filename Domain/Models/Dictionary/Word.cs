namespace Domain.Models.Dictionary
{
    public class Word
    {
        private const string WordAlreadyLearnedError = "Word '{0}' is already learned";
        private const string WordAlreadyMarkedForStudyError = "Word '{0}' is already marked for study";
        private const string ValuesNullOrEmptyError = "Values array cannot be null, empty, or contain null/whitespace elements.";

        public int Id { get; set; }

        private string[] _values = [];
        public string[] Values
        {
            get => _values;
            set
            {
                if (value == null || value.Length == 0 || value.Any(string.IsNullOrWhiteSpace))
                {
                    throw new ArgumentException(ValuesNullOrEmptyError, nameof(Values));
                }

                _values = value;
            }
        }
       
        public decimal? Popularity { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }
        public bool ShouldStudy { get; set; }
        public bool Learned { get; set; }
        public string Comments { get; set; } = string.Empty;

        public void Learn()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, Values));
            }

            Learned = true;

            ShouldStudy = false;
        }

        public void MarkForStudy()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, Values));
            }

            if (ShouldStudy)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyMarkedForStudyError, Values));
            }

            ShouldStudy = true;
        }
    }
}
