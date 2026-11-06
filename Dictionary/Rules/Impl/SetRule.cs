namespace Dictionary.Rules.Impl
{
    public class SetRule(HashSet<char> letters) : IRule
    {
        public RuleState? StartingState { get; } = letters.Count > 0 ? new(1, 0) : null;

        public RuleProcessor GetProcessor()
        {
            return new Processor(letters);
        }

        private class Processor(HashSet<char> letters) : RuleProcessor(letters.Count > 0 ? new(1, 0) : throw new ArgumentException("Letters set cannot be empty", nameof(letters)))
        {
            protected override RuleState? AddChar(char c)
            {
                return letters.Contains(c) ? new(0, 0) : null;
            }
        }
    }
}
