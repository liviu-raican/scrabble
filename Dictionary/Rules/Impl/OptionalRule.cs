namespace Dictionary.Rules.Impl
{
    public class OptionalRule(IRule rule) : IRule
    {
        public RuleState? StartingState => new(0, rule.StartingState?.MaxChars ?? 0);

        public RuleProcessor GetProcessor()
        {
            return new Processor(rule);
        }

        private class Processor(IRule rule) : RuleProcessor(new RuleState(0, rule.StartingState?.MaxChars ?? 0))
        {
            private readonly RuleProcessor ruleProcessor = rule.GetProcessor();

            protected override RuleState? AddChar(char c)
            {
                if (ruleProcessor.TryPushChar(c))
                {
                    return ruleProcessor.State;
                }
                return null;
            }

            protected override void Undo()
            {
                ruleProcessor.PopChar();
            }
        }
    }
}
