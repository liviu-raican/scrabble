namespace WordFinder.Rules.Impl
{
    public class AllRule(params IRule[] rules) : IRule
    {
        public RuleState? StartingState => Processor.GetRuleState([.. rules.Select(r => r.StartingState)]);

        public RuleProcessor GetProcessor()
        {
            return new Processor(rules);
        }

        private class Processor(params IRule[] rules) : RuleProcessor(GetRuleState([.. rules.Select(r => r.StartingState)]) ?? throw new ArgumentException("Not all rules can be satisfied", nameof(rules)))
        {
            internal static RuleState? GetRuleState(RuleState?[] innerStates)
            {
                if (innerStates.Any(s => s == null))
                {
                    return null;
                }
                
                uint minChars = innerStates.Max(s => s!.MinChars);
                uint maxChars = innerStates.Min(s => s!.MaxChars);

                if (minChars > maxChars)
                {
                    return null;
                }

                return new(MinChars: minChars, MaxRange: maxChars - minChars);
            }

            private readonly RuleProcessor[] processors = [.. rules.Select(r => r.GetProcessor())];

            protected override RuleState? AddChar(char c)
            {
                List<RuleProcessor> succesfulProcessors = [];
                foreach (var processor in processors)
                {
                    if (processor.TryPushChar(c))
                    {
                        succesfulProcessors.Add(processor);
                    }
                    else
                    {
                        break;
                    }
                }

                RuleState? nextState;
                if (succesfulProcessors.Count == processors.Length && (nextState = GetRuleState([.. succesfulProcessors.Select(p => p.State)])) != null)
                {
                    return nextState;
                }
                else
                {
                    foreach (var processor in succesfulProcessors)
                    {
                        processor.PopChar();
                    }
                    return null;
                }
            }

            protected override void Undo()
            {
                foreach (var processor in processors)
                {
                    processor.PopChar();
                }
            }
        }
    }
}
