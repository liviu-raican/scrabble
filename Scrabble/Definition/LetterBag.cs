using System.Collections.Immutable;

namespace Scrabble.Definition
{
    public record LetterBag(IReadOnlyDictionary<char, LetterDefinition> Letters, uint Jokers)
    {
        public uint StoneCount { get; } = (Letters.Count > 0 ? (uint)Letters.Sum(l => l.Value.Count) : 0) + Jokers;
        public uint TotalValue { get; } = Letters.Count > 0 ? (uint)Letters.Sum(l => l.Value.Count * l.Value.Value) : 0;
        public ImmutableSortedDictionary<uint, uint> StoneCountByValue { get; } =
            Letters.Values.Append(new LetterDefinition(Jokers, 0))
            .GroupBy(d => d.Value, d => d.Count)
            .ToImmutableSortedDictionary(g => g.Key, g => (uint)g.Sum(g => g));
    }
}
