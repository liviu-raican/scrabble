namespace Dictionary.Rules.Impl
{
    public class AnagramRule(string word)
        : AllRule(
            new MandatoryLettersRule(word),
            new LengthRule((uint)word.Length, 0)
        );
}
