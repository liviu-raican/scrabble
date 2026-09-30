using System.Data;

namespace WordFinder.Rules.Impl
{
    public class SequenceRule(params IRule[] rules) : IRule
    {
        public RuleState? StartingState => Processor.GetRuleState([.. rules.Select(r => r.StartingState)]);

        public RuleProcessor GetProcessor()
        {
            return new Processor(rules);
        }

        private class Processor : RuleProcessor
        {
            internal static RuleState? GetRuleState(IEnumerable<RuleState?> innerStates)
            {
                if (innerStates.Any(s => s == null))
                {
                    return null;
                }

                return GetNonNullRuleState(innerStates.Select(s => s!));
            }

            private static RuleState GetNonNullRuleState(IEnumerable<RuleState> innerStates)
            {
                return new(MinChars: (uint)innerStates.Sum(s => s.MinChars), MaxRange: (uint)innerStates.Sum(s => s.MaxRange));
            }

            private readonly IRule[] rules;
            private readonly RuleState[] startingStates;
            private readonly Dictionary<IRule, int> ruleIndexMap;
            private readonly Stack<List<RuleProcessor>> explorers;
            private readonly Dictionary<RuleProcessor, IRule> processorMap;

            public Processor(params IRule[] rules) : this(rules, [.. rules.Select(r => r.StartingState)])
            {
            }

            private Processor(IRule[] rules, RuleState?[] startingStates) : base(GetRuleState(startingStates) ?? throw new ArgumentException("Not all rules can be satisfied", nameof(rules)))
            {
                this.rules = rules;
                this.startingStates = [.. startingStates.Select(s => s!)];

                ruleIndexMap = rules.Select((r, i) => new KeyValuePair<IRule, int>(r, i)).ToDictionary(p => p.Key, p => p.Value);
                processorMap = [];

                explorers = [];
                List<RuleProcessor> firstProcessors = [];
                List<int> startingIndices = GetNewProcessorIndicesFromIndex();

                foreach (int index in startingIndices)
                {
                    RuleProcessor processor = rules[index].GetProcessor();
                    firstProcessors.Add(processor);
                    processorMap.Add(processor, rules[index]);
                }
                explorers.Push(firstProcessors);
            }

            protected override RuleState? AddChar(char c)
            {
                List<RuleProcessor> newExplorers = [];
                HashSet<int> newProcessorIndices = [];
                uint minChars = uint.MaxValue;
                uint maxChars = 0;
                bool pushed = false;
                foreach (RuleProcessor explorer in explorers.Peek())
                {
                    if (explorer.TryPushChar(c))
                    {
                        int index = ruleIndexMap[processorMap[explorer]] + 1;
                        if (explorer.State.Satisfied)
                        {   
                            if (index < rules.Length)
                            {
                                foreach (int newIndex in GetNewProcessorIndicesFromIndex(index))
                                {
                                    newProcessorIndices.Add(newIndex);
                                }
                            }
                        }

                        RuleState stateOfRemainingRules = GetNonNullRuleState(startingStates.Skip(index).Prepend(explorer.State));
                        minChars = uint.Min(minChars, stateOfRemainingRules.MinChars);
                        maxChars = uint.Max(maxChars, stateOfRemainingRules.MaxChars);
                        
                        if (explorer.State.CanContinue)
                        {
                            newExplorers.Add(explorer);
                        }
                        else
                        {
                            explorer.PopChar();
                        }

                        pushed = true;
                    }
                }

                if (!pushed)
                {
                    return null;
                }

                foreach (int newIndex in newProcessorIndices)
                {
                    RuleProcessor newProcessor = rules[newIndex].GetProcessor();
                    newExplorers.Add(newProcessor);
                    processorMap.Add(newProcessor, rules[newIndex]);

                    RuleState stateOfRemainingRules = GetNonNullRuleState(startingStates.Skip(newIndex));
                    minChars = uint.Min(minChars, stateOfRemainingRules.MinChars);
                    maxChars = uint.Max(maxChars, stateOfRemainingRules.MaxChars);
                }

                explorers.Push(newExplorers);
                if (newExplorers.Count > 0)
                {
                    return new(minChars, maxChars - minChars);
                }
                else
                {
                    return new(0, 0);
                }
            }

            protected override void Undo()
            {
                foreach (var processor in explorers.Pop())
                {
                    if (processor.Length > 0)
                    {
                        processor.PopChar();
                    }
                    else
                    {
                        processorMap.Remove(processor);
                    }
                }
            }

            private List<int> GetNewProcessorIndicesFromIndex(int index = 0)
            {
                List<int> newIndices = [];

                while (index < rules.Length)
                {
                    if (startingStates[index].CanContinue)
                    {
                        newIndices.Add(index);

                        if (!startingStates[index].Satisfied)
                        {
                            break;
                        }
                    }

                    index++;
                }

                return newIndices;
            }
        }
    }
}
