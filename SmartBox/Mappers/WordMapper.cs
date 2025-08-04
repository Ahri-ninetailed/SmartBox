using Shared;

namespace SmartBox.Mappers
{
    public class WordMapper : IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word>
    {
        private const string Separator = ", ";

        private readonly IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> _mapper;

        public WordMapper(IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> mapper)
        {
            _mapper = mapper;
        }

        public Models.Dictionary.Word Map(Application.Models.Dictionary.Word source)
        {
            Models.Dictionary.PartOfSpeech partOfSpeech = _mapper.Map(source.PartOfSpeech);

            return new Models.Dictionary.Word
            {
                Id = source.Id,
                Value = string.Join(Separator, source.Values),
                Popularity = source.Popularity,
                PartOfSpeech = partOfSpeech,
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments
            };
        }
    }
}
