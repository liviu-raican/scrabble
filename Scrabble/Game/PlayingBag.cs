using Scrabble.Definition;
using System.Collections.Immutable;

namespace Scrabble.Game
{
    public class PlayingBag(LetterBag fullBag)
    {
        private readonly LetterBag fullBag = fullBag;
        private readonly Dictionary<char, uint> letters = fullBag.Letters.ToDictionary(l => l.Key, l=> l.Value.Count);
        private readonly Stack<char?> removedLetters = [];
        private readonly ImmutableSortedDictionary<uint, uint>.Builder stoneCountByValueBuilder = fullBag.StoneCountByValue.ToBuilder();

        private uint jokers = fullBag.Jokers;

        public LetterBag FullBag => fullBag;
        public uint StoneCount { get; private set; } = fullBag.StoneCount;
        public uint TotalValue { get; private set; } = fullBag.TotalValue;
        public ImmutableSortedDictionary<uint, uint> StoneCountByValue => stoneCountByValueBuilder.ToImmutable();

        public uint GetLetterCount(char? letter)
        {
            return letter == null ? jokers : (letters.TryGetValue((char)letter, out uint count) ? count : 0);
        }

        public bool ExtractLetter(char? letter)
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
            if (StoneCount < size)
            {
                throw new ArgumentException("Not enough stones in bag", nameof(size));
            }

            List<char?> extracted = [];

            Random random = new();
            for (int i = 0; i < size; i++)
            {
                int stoneIndex = random.Next((int)StoneCount);

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

        public bool ReturnStone()
        {
            if (removedLetters.Count == 0)
            {
                return false;
            }

            char? lastLetter = removedLetters.Pop();
            if (lastLetter == null)
            {
                AddJoker();
            }
            else
            {
                AddLetter((char)lastLetter);
            }
            return true;
        }

        private void RemoveLetter(char letter)
        {
            StoneCount--;

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

            removedLetters.Push(letter);
        }

        private void AddLetter(char letter)
        {
            StoneCount++;

            letters[letter] = (letters.TryGetValue(letter, out uint letterCount) ? letterCount : 0) + 1;

            uint value = fullBag.Letters[letter].Value;
            TotalValue += value;

            stoneCountByValueBuilder[value] = (stoneCountByValueBuilder.TryGetValue(value, out uint valueCount) ? valueCount : 0) + 1;
        }

        private void RemoveJoker()
        {
            jokers--;
            StoneCount--;

            removedLetters.Push(null);
        }

        private void AddJoker()
        {
            jokers++;
            StoneCount++;
        }

    }
}
