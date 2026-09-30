using WordFinder.Rules;

namespace Scrabble.Rule
{
    public class AnagramRule(string word) : IRule
    {
        private static Dictionary<char, uint> GetLetterCounts(string word)
        {
            return word.GroupBy(c => c).ToDictionary(g => g.Key, g => (uint)g.Count());
        }

        public RuleState? StartingState { get; } = Processor.GetRuleState(GetLetterCounts(word));

        public RuleProcessor GetProcessor()
        {
            return new Processor(GetLetterCounts(word));
        }

        internal class Processor(Dictionary<char, uint> letters) : RuleProcessor(GetRuleState(letters))
        {
            internal static RuleState GetRuleState(Dictionary<char, uint> letters)
            {
                return new((uint)letters.Sum(l => l.Value));
            }

            private readonly Stack<char?> lettersUsed = [];

            protected override RuleState? AddChar(char c)
            {
                if (letters.TryGetValue(c, out uint available))
                {
                    lettersUsed.Push(c);
                    if (available > 1)
                    {
                        letters[c] = available - 1;
                    }
                    else
                    {
                        letters.Remove(c);
                    }
                }
                else
                {
                    lettersUsed.Push(null);
                }

                return GetRuleState(letters);
            }

            protected override void Undo()
            {
                char? c = lettersUsed.Pop();
                if (c != null)
                {
                    if (letters.TryGetValue((char)c, out uint available))
                    {
                        letters[(char)c] = available + 1;
                    }
                    else
                    {
                        letters[(char)c] = 1;
                    }
                }
            }
        }
    }
}
