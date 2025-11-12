using Shared;

namespace SmartBox.Mappers
{
    public class PartOfSpeechMapper : IMapper<Domain.Models.Dictionary.PartOfSpeech, SmartBox.Models.Dictionary.PartOfSpeech>,
                                      IMapper<SmartBox.Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Domain.Models.Parser.PartOfSpeech, SmartBox.Models.Parser.PartOfSpeech>,
                                      IMapper<SmartBox.Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Models.Dictionary.PartOfSpeech Map(Domain.Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.Dictionary.PartOfSpeech.Unknown => Models.Dictionary.PartOfSpeech.Unknown,
                Domain.Models.Dictionary.PartOfSpeech.Other => Models.Dictionary.PartOfSpeech.Other,
                Domain.Models.Dictionary.PartOfSpeech.Verb => Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Domain.Models.Dictionary.PartOfSpeech Map(Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Models.Dictionary.PartOfSpeech.Unknown => Domain.Models.Dictionary.PartOfSpeech.Unknown,
                Models.Dictionary.PartOfSpeech.Other => Domain.Models.Dictionary.PartOfSpeech.Other,
                Models.Dictionary.PartOfSpeech.Verb => Domain.Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Models.Parser.PartOfSpeech Map(Domain.Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.Parser.PartOfSpeech.Unknown => Models.Parser.PartOfSpeech.Unknown,
                Domain.Models.Parser.PartOfSpeech.Noun => Models.Parser.PartOfSpeech.Noun,
                Domain.Models.Parser.PartOfSpeech.Verb => Models.Parser.PartOfSpeech.Verb,
                Domain.Models.Parser.PartOfSpeech.PhrasalVerb => Models.Parser.PartOfSpeech.PhrasalVerb,
                Domain.Models.Parser.PartOfSpeech.Idiom => Models.Parser.PartOfSpeech.Idiom,
                Domain.Models.Parser.PartOfSpeech.BlueRectangle => Models.Parser.PartOfSpeech.BlueRectangle,
                Domain.Models.Parser.PartOfSpeech.Other => Models.Parser.PartOfSpeech.Other,
                Domain.Models.Parser.PartOfSpeech.Adjective => Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Domain.Models.Parser.PartOfSpeech Map(Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Models.Parser.PartOfSpeech.Unknown => Domain.Models.Parser.PartOfSpeech.Unknown,
                Models.Parser.PartOfSpeech.Noun => Domain.Models.Parser.PartOfSpeech.Noun,
                Models.Parser.PartOfSpeech.Verb => Domain.Models.Parser.PartOfSpeech.Verb,
                Models.Parser.PartOfSpeech.PhrasalVerb => Domain.Models.Parser.PartOfSpeech.PhrasalVerb,
                Models.Parser.PartOfSpeech.Idiom => Domain.Models.Parser.PartOfSpeech.Idiom,
                Models.Parser.PartOfSpeech.BlueRectangle => Domain.Models.Parser.PartOfSpeech.BlueRectangle,
                Models.Parser.PartOfSpeech.Other => Domain.Models.Parser.PartOfSpeech.Other,
                Models.Parser.PartOfSpeech.Adjective => Domain.Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }
    }
}