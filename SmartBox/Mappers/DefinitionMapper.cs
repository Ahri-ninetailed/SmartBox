using Application.Models.Parser;
using Shared;
using SmartBox.Models.Parser;

namespace SmartBox.Mappers
{
    public class DefinitionMapper : IMapper<SmartBox.Models.Parser.Meaning, Application.Models.Parser.Definition>
    {
        public Definition Map(Meaning source)
        {
            return new Definition()
            {
                Value = source.Value,
                Id = source.Id,
                Examples = source.Children.Select(example =>
                {
                    return new Application.Models.Parser.Example()
                    {
                        Id = example.Id,
                        Value = example.Value
                    };
                }).ToList()
            };
        }
    }
}
