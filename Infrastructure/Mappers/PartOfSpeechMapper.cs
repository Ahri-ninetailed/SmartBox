using Shared;

namespace Infrastructure.Mappers
{
    public class PartOfSpeechMapper : IMapper<Models.DataBase.PartOfSpeech?, Domain.Models.Dictionary.PartOfSpeech>,
                                      IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.DataBase.PartOfSpeech?>,
                                      IMapper<Models.Parser.PartsOfSpeech, Domain.Models.Parser.PartsOfSpeech>
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public Domain.Models.Dictionary.PartOfSpeech Map(Models.DataBase.PartOfSpeech? source)
        {
            switch (source)
            {
                case Models.DataBase.PartOfSpeech.Noun:
                case Models.DataBase.PartOfSpeech.Other:
                    return Domain.Models.Dictionary.PartOfSpeech.Other;
                case Models.DataBase.PartOfSpeech.Verb:
                    return Domain.Models.Dictionary.PartOfSpeech.Verb;
                case null:
                    return Domain.Models.Dictionary.PartOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }

        public Models.DataBase.PartOfSpeech? Map(Domain.Models.Dictionary.PartOfSpeech source)
        {
            switch (source)
            {
                case Domain.Models.Dictionary.PartOfSpeech.Verb:
                    return Models.DataBase.PartOfSpeech.Verb;
                case Domain.Models.Dictionary.PartOfSpeech.Other:
                    return Models.DataBase.PartOfSpeech.Other;
                case Domain.Models.Dictionary.PartOfSpeech.Unknown:
                    return null;// TODO: сделать в бд такой же тип частей речей как и в Domain
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }

        public Domain.Models.Parser.PartsOfSpeech Map(Models.Parser.PartsOfSpeech source)
        {
            switch (source)
            {
                case Models.Parser.PartsOfSpeech.Noun:
                    return Domain.Models.Parser.PartsOfSpeech.Noun;
                case Models.Parser.PartsOfSpeech.Verb:
                    return Domain.Models.Parser.PartsOfSpeech.Verb;
                case Models.Parser.PartsOfSpeech.PhrasalVerb:
                    return Domain.Models.Parser.PartsOfSpeech.PhrasalVerb;
                case Models.Parser.PartsOfSpeech.Idiom:
                    return Domain.Models.Parser.PartsOfSpeech.Idiom;
                case Models.Parser.PartsOfSpeech.BlueRectangle:
                    return Domain.Models.Parser.PartsOfSpeech.BlueRectangle;
                case Models.Parser.PartsOfSpeech.Other:
                    return Domain.Models.Parser.PartsOfSpeech.Other;
                case Models.Parser.PartsOfSpeech.Unknown:
                    return Domain.Models.Parser.PartsOfSpeech.Unknown;
                default:
                    throw new InvalidOperationException(string.Format(UnknownEnumValueError, source));
            }
        }
    }
}
