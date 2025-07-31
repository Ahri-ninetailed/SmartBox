namespace Domain.Models
{
    public class Word
    {
        private const string WordAlreadyLearnedError = "Word '{0}' is already learned";
        private const string WordAlreadyMarkedForStudyError = "Word '{0}' is already marked for study";

        public int Id { get; set; }
        public required string Value { get; set; }
        public decimal? Popularity { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; }
        public bool ShouldStudy { get; set; }
        public bool Learned { get; set; }
        public string? Comments { get; set; }

        public void Learn()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, Value));
            }

            Learned = true;

            ShouldStudy = false;
        }

        public void MarkForStudy()
        {
            if (Learned)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyLearnedError, Value));
            }

            if (ShouldStudy)
            {
                throw new InvalidOperationException(string.Format(WordAlreadyMarkedForStudyError, Value));
            }

            ShouldStudy = true;
        }
    }
}
