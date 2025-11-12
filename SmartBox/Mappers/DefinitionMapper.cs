using Shared;
using SmartBox.Models.Parser;

namespace SmartBox.Mappers
{
    public class DefinitionMapper : IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Dictionary.Definition>,
                                     IMapper<SmartBox.Models.Parser.Meaning, Domain.Models.Parser.Definition>
    {
        public Domain.Models.Dictionary.Definition Map(Meaning source)
        {
            return new Domain.Models.Dictionary.Definition()
            {
                Value = source.Value.ToLowerFirstChar(),
                Id = source.Id,
                Examples = source.Children.Select(example =>
                {
                    return new Domain.Models.Dictionary.Example()
                    {
                        Id = example.Id,
                        DefinitionId = source.Id,
                        Value = example.Value.ToLowerFirstChar(),
                    };
                }).ToList()
            };
        }

        Domain.Models.Parser.Definition IMapper<Meaning, Domain.Models.Parser.Definition>.Map(Meaning source)
        {
            return new Domain.Models.Parser.Definition()
            {
                Value = source.Value.ToLowerFirstChar(),
                Id = source.Id,
                Examples = source.Children.Select(example =>
                {
                    return new Domain.Models.Parser.Example()
                    {
                        Id = example.Id,
                        Value = example.Value.ToLowerFirstChar(),
                    };
                }).ToList()
            };
        }
    }
}
