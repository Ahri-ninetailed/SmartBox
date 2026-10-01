using Domain.Models.Dictionary;
using Domain.Rules;

namespace Domain.Tests.Tests
{
    public class WordValuesTests
    {
        [Theory]
        [InlineData("  Odds and Ends ", "odds and ends")]
        [InlineData("RUN", "run")]
        [InlineData("run", "run")]
        public void Normalize_Value_LowercasedAndTrimmed(string value, string expected)
        {
            Assert.Equal(expected, WordValues.Normalize(value));
        }

        [Fact]
        public void CreateNew_ValuesWithCapitalsAndSpaces_Normalized()
        {
            Word word = Word.CreateNew([" Antechamber", "ANTEROOM "], PartOfSpeech.Other, "comment");

            Assert.Equal(["antechamber", "anteroom"], word.Values);
            Assert.Equal(PartOfSpeech.Other, word.PartOfSpeech);
            Assert.Equal("comment", word.Comments);
        }

        [Theory]
        [InlineData(new object[] { new string[0] })]
        [InlineData(new object[] { new[] { "run", "   " } })]
        [InlineData(new object[] { new[] { "run", null } })]
        public void CreateNew_EmptyValues_Throws(string[] values)
        {
            Assert.Throws<ArgumentException>(() => Word.CreateNew(values, PartOfSpeech.Other, ""));
        }

        [Fact]
        public void Values_SetDirectly_KeptAsIs()
        {
            // Так слово собирается из БД: сохраненные значения не переписываем
            Word word = new() { Values = ["saint", "St"] };

            Assert.Equal(["saint", "St"], word.Values);
        }
    }
}
