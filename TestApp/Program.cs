// See https://aka.ms/new-console-template for more information
using Scrabble.Definition;
using Scrabble.Rule;
using System.Diagnostics;
using WordFinder.Dictionary;
using WordFinder.Rules.Impl;

DawgxDictionary dictionary = new (File.ReadAllBytes("dictionary.dawgx"));

var sw = Stopwatch.StartNew();

/*Console.WriteLine(dictionary.WordExists("BABA"));
Console.WriteLine(dictionary.WordExists("BA"));
Console.WriteLine(dictionary.WordExists("ABA"));
Console.WriteLine(dictionary.WordExists("ABAC"));
Console.WriteLine(dictionary.WordExists("AAA"));
Console.WriteLine(dictionary.WordExists(""));
Console.WriteLine(dictionary.WordExists("A"));
Console.WriteLine(dictionary.WordExists("AVERF"));
Console.WriteLine(dictionary.WordExists("CACA"));
Console.WriteLine(dictionary.WordExists("ABC"));
Console.WriteLine(dictionary.WordExists("PREFIGURATIVELE"));
Console.WriteLine(dictionary.WordExists("PREFIGURATIVEL"));
Console.WriteLine(dictionary.WordExists("PREFIGURATIVELOR"));
Console.WriteLine(dictionary.WordExists("AALENIAN"));
Console.WriteLine(dictionary.WordExists("AALENIENI"));
Console.WriteLine(dictionary.WordExists("BACLAVAUA"));
Console.WriteLine(dictionary.WordExists("MANCE"));
Console.WriteLine(dictionary.WordExists("CULUI"));*/

LetterBag myBag = new(
    new Dictionary<char, LetterDefinition>()
    {
        ['A'] = new(3, 1),
        ['M'] = new(1, 4),
        ['Z'] = new(3, 10),
        ['I'] = new(4, 1)
    },
    0
    );

foreach (string word in dictionary.FindWords(
    new SequenceRule(
            new WildcardRule(),
            new AnyRule(
                new StringRule("AAA"),
                new StringRule("EEE"),
                //new StringRule("III"),
                new StringRule("OOO"),
                new StringRule("UUU")
            ),
            new WildcardRule()
        )
    )
    )
{
    Console.WriteLine(word);
}

sw.Stop();
Console.WriteLine($"Elapsed: {sw.Elapsed}");
