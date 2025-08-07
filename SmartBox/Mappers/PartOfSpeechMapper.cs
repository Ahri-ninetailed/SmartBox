using Shared;

namespace SmartBox.Mappers
{
    public class PartOfSpeechMapper : IMapper<Application.Models.Dictionary.PartOfSpeech, SmartBox.Models.Dictionary.PartOfSpeech>,
                                      IMapper<SmartBox.Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Application.Models.Parser.PartOfSpeech, SmartBox.Models.Parser.PartOfSpeech>,
                                      IMapper<SmartBox.Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech>
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

        public Models.Parser.PartOfSpeech Map(Application.Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Application.Models.Parser.PartOfSpeech.Unknown => Models.Parser.PartOfSpeech.Unknown,
                Application.Models.Parser.PartOfSpeech.Noun => Models.Parser.PartOfSpeech.Noun,
                Application.Models.Parser.PartOfSpeech.Verb => Models.Parser.PartOfSpeech.Verb,
                Application.Models.Parser.PartOfSpeech.PhrasalVerb => Models.Parser.PartOfSpeech.PhrasalVerb,
                Application.Models.Parser.PartOfSpeech.Idiom => Models.Parser.PartOfSpeech.Idiom,
                Application.Models.Parser.PartOfSpeech.BlueRectangle => Models.Parser.PartOfSpeech.BlueRectangle,
                Application.Models.Parser.PartOfSpeech.Other => Models.Parser.PartOfSpeech.Other,
                Application.Models.Parser.PartOfSpeech.Adjective => Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Application.Models.Parser.PartOfSpeech Map(Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Models.Parser.PartOfSpeech.Unknown => Application.Models.Parser.PartOfSpeech.Unknown,
                Models.Parser.PartOfSpeech.Noun => Application.Models.Parser.PartOfSpeech.Noun,
                Models.Parser.PartOfSpeech.Verb => Application.Models.Parser.PartOfSpeech.Verb,
                Models.Parser.PartOfSpeech.PhrasalVerb => Application.Models.Parser.PartOfSpeech.PhrasalVerb,
                Models.Parser.PartOfSpeech.Idiom => Application.Models.Parser.PartOfSpeech.Idiom,
                Models.Parser.PartOfSpeech.BlueRectangle => Application.Models.Parser.PartOfSpeech.BlueRectangle,
                Models.Parser.PartOfSpeech.Other => Application.Models.Parser.PartOfSpeech.Other,
                Models.Parser.PartOfSpeech.Adjective => Application.Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}