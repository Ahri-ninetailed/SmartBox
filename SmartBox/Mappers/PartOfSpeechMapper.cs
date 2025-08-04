using Shared;

namespace SmartBox.Mappers
{
    public class PartOfSpeechMapper : IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech>,
                                      IMapper<Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.Dictionary.PartOfSpeech Map(Application.Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Application.Models.Dictionary.PartOfSpeech.Unknown => Models.Dictionary.PartOfSpeech.Unknown,
                Application.Models.Dictionary.PartOfSpeech.Other => Models.Dictionary.PartOfSpeech.Other,
                Application.Models.Dictionary.PartOfSpeech.Verb => Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Application.Models.Dictionary.PartOfSpeech Map(Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Models.Dictionary.PartOfSpeech.Unknown => Application.Models.Dictionary.PartOfSpeech.Unknown,
                Models.Dictionary.PartOfSpeech.Other => Application.Models.Dictionary.PartOfSpeech.Other,
                Models.Dictionary.PartOfSpeech.Verb => Application.Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}