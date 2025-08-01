using Shared;
using Infrastructure.Models;

namespace Infrastructure.Mappers
{
    public class WordMapper : IMapper<Word, Domain.Models.Word>, 
                              IMapper<Domain.Models.Word, Word>
    {
        private readonly IMapper<PartOfSpeech?, Domain.Models.PartOfSpeech> _partOfSpeechToDomMapper;
        private readonly IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?> _partOfSpeechToInfsMapper;

        public WordMapper(IMapper<PartOfSpeech?, Domain.Models.PartOfSpeech> partOfSpeechToDomMapper,
                          IMapper<Domain.Models.PartOfSpeech, PartOfSpeech?> partOfSpeechToInfsMapper)
        {
            _partOfSpeechToDomMapper = partOfSpeechToDomMapper;
            _partOfSpeechToInfsMapper = partOfSpeechToInfsMapper;
        }
        public Domain.Models.Word Map(Word source)
        {
            return new Domain.Models.Word()
            {
                Id = source.Id,
                Value = source.Value,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToDomMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments ?? string.Empty
            };
        }

        public Word Map(Domain.Models.Word source)
        {
            return new Word()
            {
                Id = source.Id,
                Value = source.Value,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToInfsMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments
            };
        }
    }
}
