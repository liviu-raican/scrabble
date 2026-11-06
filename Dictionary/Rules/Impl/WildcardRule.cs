namespace Dictionary.Rules.Impl
{
    public class WildcardRule() : IRule
    {
        public RuleState? StartingState { get; } = new(0);

        public RuleProcessor GetProcessor()
        {
            return new Processor();
        }

        private class Processor() : RuleProcessor(new(0))
        {
            protected override RuleState? AddChar(char c)
            {
                return new(0);
            }
        }
    }
}
