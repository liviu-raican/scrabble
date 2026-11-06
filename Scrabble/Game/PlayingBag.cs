using Scrabble.Definition;
using System.Collections.Immutable;

namespace Scrabble.Game
{
    public class PlayingBag
    {
        private readonly LetterBag fullBag;
        private readonly Dictionary<char, uint> letters;
        private readonly ImmutableSortedDictionary<uint, uint>.Builder stoneCountByValueBuilder;

        private uint jokers;

        public LetterBag FullBag => fullBag;
        public uint TotalStones { get;
            private set; }
        public uint TotalValue { get; private set; }
        public ImmutableSortedDictionary<uint, uint> StoneCountByValue => stoneCountByValueBuilder.ToImmutable();

        public PlayingBag(LetterBag fullBag)
        {
            this.fullBag = fullBag;
            
            letters = fullBag.Letters.ToDictionary(l => l.Key, l => l.Value.Count);
            jokers = fullBag.Jokers;
            stoneCountByValueBuilder = fullBag.Letters.Values.Append(new LetterDefinition(fullBag.Jokers, 0))
                .GroupBy(d => d.Value, d => d.Count)
                .ToImmutableSortedDictionary(g => g.Key, g => (uint)g.Sum(g => g)).ToBuilder();

            TotalStones = (fullBag.Letters.Count > 0 ? (uint)fullBag.Letters.Sum(l => l.Value.Count) : 0) + fullBag.Jokers;
            TotalValue = fullBag.Letters.Count > 0 ? (uint)fullBag.Letters.Sum(l => l.Value.Count * l.Value.Value) : 0;
        }

        public PlayingBag(LetterBag fullBag, IEnumerable<char?> subset)
        {
            this.fullBag = fullBag;

            letters = subset.OfType<char>().GroupBy(c => c).ToDictionary(g => g.Key, g => (uint)g.Count());
            jokers = (uint)subset.Count(c => c == null);

            if (jokers > fullBag.Jokers || letters.Any(entry => !fullBag.Letters.TryGetValue(entry.Key, out LetterDefinition? bagCount) || bagCount.Count < entry.Value))
            {
                throw new Exception("Stones not available in this bag");
            }

            stoneCountByValueBuilder = letters
                .Select(entry => new KeyValuePair<char?, uint>(entry.Key, entry.Value))
                .Append(new(null, jokers))
                .GroupBy(entry => entry.Key == null ? 0 : fullBag.Letters[(char)entry.Key].Value)
                .ToImmutableSortedDictionary(g => g.Key, g => (uint)g.Sum(entry => entry.Value)).ToBuilder();

            TotalStones = (uint)subset.Count();
            TotalValue = (uint)letters.Sum(entry => fullBag.Letters[entry.Key].Value * entry.Value);
        }

        public PlayingBag(PlayingBag bagToClone)
        {
            fullBag = bagToClone.FullBag;

            letters = new(bagToClone.letters);
            stoneCountByValueBuilder = bagToClone.StoneCountByValue.ToBuilder();
            jokers = bagToClone.jokers;

            TotalStones = bagToClone.TotalStones;
            TotalValue = bagToClone.TotalValue;
        }

        public uint GetStoneCount(char? letter)
        {
            return letter == null ? jokers : (letters.TryGetValue((char)letter, out uint count) ? count : 0);
        }

        public bool HasStones(IEnumerable<char?> stones)
        {
            Dictionary<char, uint> stoneCounts = [];
            uint jokerCount = 0;
            foreach (char? stone in stones)
            {
                if (stone == null)
                {
                    jokerCount++;
                }
                else
                {
                    if (stoneCounts.ContainsKey((char)stone))
                    {
                        stoneCounts[(char)stone]++;
                    }
                    else
                    {
                        stoneCounts[(char)stone] = 1;
                    }
                }
            }
            foreach (var entry in stoneCounts)
            {
                if (GetStoneCount(entry.Key) < entry.Value)
                {
                    return false;
                }
            }
            return GetStoneCount(null) >= jokerCount;
        }

        public bool ExtractStone(char? letter)
        {
            if (letter == null)
            {
                if (jokers > 0)
                {
                    RemoveJoker();

                    return true;
                }
                return false;
            }
            else
            {
                if (letters.TryGetValue((char)letter, out uint count) && count > 0)
                {
                    RemoveLetter((char)letter);

                    return true;
                }
                return false;
            }
        }

        public List<char?> ExtractRandomGroup(uint size)
        {
            if (TotalStones < size)
            {
                throw new ArgumentException("Not enough stones in bag", nameof(size));
            }

            List<char?> extracted = [];

            Random random = new();
            for (int i = 0; i < size; i++)
            {
                int stoneIndex = random.Next((int)TotalStones);

                if (stoneIndex < jokers)
                {
                    RemoveJoker();
                    extracted.Add(null);

                    continue;
                }

                uint count = jokers;
                var letterEntry = letters.First(entry => (count += entry.Value) > stoneIndex);

                RemoveLetter(letterEntry.Key);
                extracted.Add(letterEntry.Key);
            }

            return extracted;
        }

        public bool InsertStone(char? letter)
        {
            uint value;
            if (letter == null)
            {
                if (fullBag.Jokers > jokers)
                {
                    jokers++;
                    value = 0;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!fullBag.Letters.TryGetValue((char)letter, out LetterDefinition? definition))
                {
                    return false;
                }

                if (!letters.TryGetValue((char)letter, out uint letterCount))
                {
                    letterCount = 0;
                }

                if (letterCount >= definition.Count)
                {
                    return false;
                }
                letters[(char)letter] = letterCount + 1;

                value = definition.Value;
                TotalValue += value;
            }

            stoneCountByValueBuilder[value] = (stoneCountByValueBuilder.TryGetValue(value, out uint valueCount) ? valueCount : 0) + 1;
            TotalStones++;
            return true;
        }

        private void RemoveLetter(char letter)
        {
            TotalStones--;

            uint letterCount = letters[letter];
            if (letterCount == 1)
            {
                letters.Remove(letter);
            }
            else
            {
                letters[letter] = letterCount - 1;
            }

            uint value = fullBag.Letters[letter].Value;
            TotalValue -= value;
            uint valueCount = stoneCountByValueBuilder[value];
            if (valueCount == 1)
            {
                stoneCountByValueBuilder.Remove(value);
            }
            else
            {
                stoneCountByValueBuilder[value] = valueCount - 1;
            }
        }

        private void RemoveJoker()
        {
            jokers--;
            TotalStones--;
            if (jokers > 0)
            {
                stoneCountByValueBuilder[0] = jokers;
            }
            else
            {
                stoneCountByValueBuilder.Remove(0);
            }
        }
    }
}
