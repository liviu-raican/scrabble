using System.Collections.Immutable;

namespace Scrabble.Definition
{
    public record LetterBag(IReadOnlyDictionary<char, LetterDefinition> Letters, uint Jokers);
}
