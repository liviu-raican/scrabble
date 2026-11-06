namespace Dictionary.Rules.Impl
{
    public class CharRule : IRule
    {
        public RuleState? StartingState { get; } = new(1, 0);

        public RuleProcessor GetProcessor()
        {
            return new Processor();
        }

        private class Processor() : RuleProcessor(new(1, 0))
        {
            protected override RuleState? AddChar(char c)
            {
                return new(0, 0);
            }
        }
    }
}
