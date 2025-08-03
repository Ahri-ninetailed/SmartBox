using Shared;

namespace SmartBox.Mappers
{
    public class WordMapper : IMapper<Application.Models.Word, Models.Word>
    {
        private const string Separator = ", ";

        private readonly IMapper<Application.Models.PartOfSpeech, Models.PartOfSpeech> _mapper;

        public WordMapper(IMapper<Application.Models.PartOfSpeech, Models.PartOfSpeech> mapper)
        {
            _mapper = mapper;
        }

        public Models.Word Map(Application.Models.Word source)
        {
            Models.PartOfSpeech partOfSpeech = _mapper.Map(source.PartOfSpeech);

            return new Models.Word
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
