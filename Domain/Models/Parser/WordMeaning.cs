namespace Domain.Models.Parser
{
    /// <summary>
    /// Пара слово и значение. Record, потому что нужно сравнение по значению: пара используется как элемент HashSet и ключ словаря
    /// </summary>
    public record WordMeaning(string Word, string Meaning);
}
