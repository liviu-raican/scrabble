namespace Dictionary.Rules.Impl
{
    public class AnyRule(params IEnumerable<IRule> rules) : IRule
    {
        public RuleState? StartingState => Processor.GetRuleState([.. rules.Select(r => r.StartingState)]);

        public RuleProcessor GetProcessor()
        {
            return new Processor(rules);
        }

        private class Processor : RuleProcessor
        {
            internal static RuleState? GetRuleState(RuleState?[] innerStates)
            {
                IEnumerable<RuleState> nonNullStates = innerStates.OfType<RuleState>();
                if (nonNullStates.Any())
                {
                    uint minChars = nonNullStates.Min(s => s.MinChars);
                    return new(MinChars: minChars, MaxRange: nonNullStates.Max(s => s.MaxChars) - minChars);
                }

                return null;
            }

            private readonly Stack<IEnumerable<RuleProcessor>> activeProcessors;

            public Processor(params IEnumerable<IRule> rules) : base(GetRuleState([.. rules.Select(r => r.StartingState)]) ?? throw new ArgumentException("No rule can be satisfied", nameof(rules)))
            {
                activeProcessors = [];
                activeProcessors.Push([.. rules.Select(r => r.GetProcessor())]);
            }

            protected override RuleState? AddChar(char c)
            {
                List<RuleProcessor> survivingProcessors = [];
                foreach (var processor in activeProcessors.Peek())
                {
                    if (processor.TryPushChar(c))
                    {
                        survivingProcessors.Add(processor);
                    }
                }

                if (survivingProcessors.Count > 0)
                {
                    activeProcessors.Push(survivingProcessors);

                    return GetRuleState([.. survivingProcessors.Select(p => p.State)]);
                }
                else
                {
                    return null;
                }
            }

            protected override void Undo()
            {
                foreach (var processor in activeProcessors.Pop())
                {
                    processor.PopChar();
                }
            }
        }
    }
}
