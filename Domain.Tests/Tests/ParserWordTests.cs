using Domain.Models.Parser;

namespace Domain.Tests.Tests
{
    public class ParserWordTests
    {
        [Fact]
        public void ValueAndOtherForms_WordWithVariantsAndPlural_ReturnsValueThenVariantsWithoutPlural()
        {
            Word word = new()
            {
                Value = "under way",
                OtherForms = ["underway"],
                PluralForms = ["under ways"]
            };

            Assert.Equal(["under way", "underway"], word.ValueAndOtherForms);
        }

        [Fact]
        public void ValueAndOtherForms_NoVariants_ReturnsOnlyValue()
        {
            Assert.Equal(["run"], new Word { Value = "run" }.ValueAndOtherForms);
        }
    }
}
