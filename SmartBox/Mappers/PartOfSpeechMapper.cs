using Shared;

namespace SmartBox.Mappers
{
    public class PartOfSpeechMapper : IMapper<Application.Models.PartOfSpeech, Models.PartOfSpeech>,
                                      IMapper<Models.PartOfSpeech, Application.Models.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.PartOfSpeech Map(Application.Models.PartOfSpeech source)
        {
            return source switch
            {
                Application.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Application.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Application.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Application.Models.PartOfSpeech Map(Models.PartOfSpeech source)
        {
            return source switch
            {
                Models.PartOfSpeech.Unknown => Application.Models.PartOfSpeech.Unknown,
                Models.PartOfSpeech.Other => Application.Models.PartOfSpeech.Other,
                Models.PartOfSpeech.Verb => Application.Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}