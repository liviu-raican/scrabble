using Dictionary;
using Scrabble.Definition;

namespace Scrabble.RO
{
    public static class RomanianGame
    {
        public static readonly LetterBag Bag = new(
            new Dictionary<char, LetterDefinition>()
            {
                ['A'] = new(11, 1),
                ['B'] = new(2, 9),
                ['C'] = new(5, 1),
                ['D'] = new(4, 2),
                ['E'] = new(9, 1),
                ['F'] = new(2, 8),
                ['G'] = new(2, 9),
                ['H'] = new(1, 10),
                ['I'] = new(10, 1),
                ['J'] = new(1, 10),
                ['L'] = new(4, 1),
                ['M'] = new(3, 4),
                ['N'] = new(6, 1),
                ['O'] = new(5, 1),
                ['P'] = new(4, 2),
                ['R'] = new(7, 1),
                ['S'] = new(5, 1),
                ['T'] = new(7, 1),
                ['U'] = new(6, 1),
                ['V'] = new(2, 8),
                ['X'] = new(1, 10),
                ['Z'] = new(1, 10)
            },
            2
            );

        public static readonly DawgxDictionary Dictionary = new(LoadDictionaryBytes());

        private static byte[] LoadDictionaryBytes()
        {
            using Stream stream = typeof(RomanianGame).Assembly.GetManifestResourceStream("dictionary.dawgx")
                ?? throw new InvalidOperationException("The Romanian dictionary resource was not found.");
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
