namespace WordFinder.Rules.Impl
{
    public class StringRule(string exactString) : IRule
    {
        public RuleState? StartingState { get; } = new((uint)exactString.Length, 0);

        public RuleProcessor GetProcessor()
        {
            return new Processor(exactString);
        }

        private class Processor(string exactString) : RuleProcessor(new((uint)exactString.Length, 0))
        {
            protected override RuleState? AddChar(char c)
            {
                if (exactString[Length] == c)
                {
                    return new((uint)(exactString.Length - Length - 1), 0);
                }

                return null;
            }
        }
    }
}
