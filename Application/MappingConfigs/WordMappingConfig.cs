using Mapster;

namespace Application.MappingConfigs
{
    public class WordMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Domain.Models.Word, Models.Word>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Value, src => src.Value)
                .Map(dest => dest.Popularity, src => src.Popularity)
                .Map(dest => dest.PartOfSpeech, src => src.PartOfSpeech)
                .Map(dest => dest.ShouldStudy, src => src.ShouldStudy)
                .Map(dest => dest.Learned, src => src.Learned)
                .Map(dest => dest.Comments, src => src.Comments);
        }
    }
}
