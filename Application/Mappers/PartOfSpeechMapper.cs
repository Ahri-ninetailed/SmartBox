using Shared;

namespace Application.Mappers
{
    public class PartOfSpeechMapper : IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Application.Models.Dictionary.PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Domain.Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech>,
                                      IMapper<Application.Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech>,
                                      IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Parser.PartOfSpeech>

    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Application.Models.Dictionary.PartOfSpeech Map(Domain.Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.Dictionary.PartOfSpeech.Unknown => Application.Models.Dictionary.PartOfSpeech.Unknown,
                Domain.Models.Dictionary.PartOfSpeech.Other => Application.Models.Dictionary.PartOfSpeech.Other,
                Domain.Models.Dictionary.PartOfSpeech.Verb => Application.Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Domain.Models.Dictionary.PartOfSpeech Map(Application.Models.Dictionary.PartOfSpeech source)
        {
            return source switch
            {
                Application.Models.Dictionary.PartOfSpeech.Unknown => Domain.Models.Dictionary.PartOfSpeech.Unknown,
                Application.Models.Dictionary.PartOfSpeech.Other => Domain.Models.Dictionary.PartOfSpeech.Other,
                Application.Models.Dictionary.PartOfSpeech.Verb => Domain.Models.Dictionary.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Application.Models.Parser.PartOfSpeech Map(Domain.Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Domain.Models.Parser.PartOfSpeech.Other => Application.Models.Parser.PartOfSpeech.Other,
                Domain.Models.Parser.PartOfSpeech.BlueRectangle => Application.Models.Parser.PartOfSpeech.BlueRectangle,
                Domain.Models.Parser.PartOfSpeech.Idiom => Application.Models.Parser.PartOfSpeech.Idiom,
                Domain.Models.Parser.PartOfSpeech.Noun => Application.Models.Parser.PartOfSpeech.Noun,
                Domain.Models.Parser.PartOfSpeech.PhrasalVerb => Application.Models.Parser.PartOfSpeech.PhrasalVerb,
                Domain.Models.Parser.PartOfSpeech.Unknown => Application.Models.Parser.PartOfSpeech.Unknown,
                Domain.Models.Parser.PartOfSpeech.Verb => Application.Models.Parser.PartOfSpeech.Verb,
                Domain.Models.Parser.PartOfSpeech.Adjective => Application.Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        public Domain.Models.Parser.PartOfSpeech Map(Application.Models.Parser.PartOfSpeech source)
        {
            return source switch
            {
                Application.Models.Parser.PartOfSpeech.Other => Domain.Models.Parser.PartOfSpeech.Other,
                Application.Models.Parser.PartOfSpeech.BlueRectangle => Domain.Models.Parser.PartOfSpeech.BlueRectangle,
                Application.Models.Parser.PartOfSpeech.Idiom => Domain.Models.Parser.PartOfSpeech.Idiom,
                Application.Models.Parser.PartOfSpeech.Noun => Domain.Models.Parser.PartOfSpeech.Noun,
                Application.Models.Parser.PartOfSpeech.PhrasalVerb => Domain.Models.Parser.PartOfSpeech.PhrasalVerb,
                Application.Models.Parser.PartOfSpeech.Unknown => Domain.Models.Parser.PartOfSpeech.Unknown,
                Application.Models.Parser.PartOfSpeech.Verb => Domain.Models.Parser.PartOfSpeech.Verb,
                Application.Models.Parser.PartOfSpeech.Adjective => Domain.Models.Parser.PartOfSpeech.Adjective,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, source)),
            };
        }

        Application.Models.Parser.PartOfSpeech IMapper<Domain.Models.Dictionary.PartOfSpeech, Application.Models.Parser.PartOfSpeech>.Map(Domain.Models.Dictionary.PartOfSpeech source)
        {
            switch (source)
            {
                case Domain.Models.Dictionary.PartOfSpeech.Other:
                    return Application.Models.Parser.PartOfSpeech.Other;
                case Domain.Models.Dictionary.PartOfSpeech.Verb:
                    return Application.Models.Parser.PartOfSpeech.Verb;
                case Domain.Models.Dictionary.PartOfSpeech.Unknown:
                    return Application.Models.Parser.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }
    }
}
