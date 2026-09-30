using WordFinder.Dictionary;

namespace WordFinder.Rules.Impl
{
    public class WordRule(DawgxDictionary dictionary) : IRule
    {
        public RuleState? StartingState { get; } = new(1);

        public RuleProcessor GetProcessor()
        {
            return new Processor(dictionary);
        }

        private class Processor(DawgxDictionary dictionary) : RuleProcessor(new(1))
        {
            private readonly Traverser traverser = dictionary.GetTraverser();

            protected override RuleState? AddChar(char c)
            {
                var (foundTransit, foundWord) = traverser.MoveToLetter(c);

                return foundTransit || foundWord ? new(foundWord ? 0 : 1u) : null;
            }

            protected override void Undo()
            {
                traverser.MoveBack();
            }
        }
    }
}
