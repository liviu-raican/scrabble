HashSet<string> words = [.. File.ReadAllLines("loc5.txt")];

var reversableWords = words.Where(
    word =>
    {
        string rev = new([.. word.Reverse()]);

        return rev == word || words.Contains(rev);
    }
    );

Console.WriteLine($"Reversable Words: {reversableWords.Count()}");

// File.WriteAllLines("loc5-reversibil-distinct.txt", reversableWords);

foreach(var word in reversableWords.Where(word => word.Length > 6))
{
    Console.WriteLine(word);
}

Console.ReadLine();