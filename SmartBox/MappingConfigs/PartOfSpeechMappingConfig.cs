using Mapster;

namespace SmartBox.MappingConfigs
{
    public class PartOfSpeechMappingConfig : IRegister
    {
        private const string UnknownEnumValueError = "Unknown enum value: {0}";

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Application.Models.PartOfSpeech, Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
            
            config.NewConfig<Models.PartOfSpeech, Application.Models.PartOfSpeech>().MapWith(src => MapPartOfSpeech(src));
        }

        public static Models.PartOfSpeech MapPartOfSpeech(Application.Models.PartOfSpeech src)
        {
            return src switch
            {
                Application.Models.PartOfSpeech.Unknown => Models.PartOfSpeech.Unknown,
                Application.Models.PartOfSpeech.Other => Models.PartOfSpeech.Other,
                Application.Models.PartOfSpeech.Verb => Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, src)),
            };
        }

        public static Application.Models.PartOfSpeech MapPartOfSpeech(Models.PartOfSpeech src)
        {
            return src switch
            {
                Models.PartOfSpeech.Unknown => Application.Models.PartOfSpeech.Unknown,
                Models.PartOfSpeech.Other => Application.Models.PartOfSpeech.Other,
                Models.PartOfSpeech.Verb => Application.Models.PartOfSpeech.Verb,
                _ => throw new InvalidOperationException(string.Format(UnknownEnumValueError, src)),
            };
        }
    }
}