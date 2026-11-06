using Dictionary;
using Dictionary.Rules;
using Dictionary.Rules.Impl;

namespace Scrabble.Rule
{
    public class CrosswordRule(string prefix, string suffix, DawgxDictionary dictionary, IRule? condition = null) : IRule
    {
        private static RuleProcessor? GetCrossProcessor(string prefix, DawgxDictionary dictionary, IRule? condition = null)
        {
            RuleProcessor processor = ((IRule)(condition == null ? new WordRule(dictionary) : new AllRule(new WordRule(dictionary), condition))).GetProcessor();
            
            foreach (char c in prefix)
            {
                if (!processor.TryPushChar(c))
                {
                    return null;
                }
            }

            return processor;
        }

        private readonly RuleProcessor? crossProcessor = GetCrossProcessor(prefix, dictionary, condition);
        private readonly string suffix = suffix;

        public RuleState? StartingState { get; } = new(1, 0);

        public RuleProcessor GetProcessor()
        {
            return new Processor(crossProcessor ?? throw new ArgumentException("The prefix cannot be used"), suffix);
        }

        private class Processor(RuleProcessor crossProcessor, string suffix) : RuleProcessor(new(1, 0))
        {
            protected override RuleState? AddChar(char c)
            {
                uint charsPushed = 0;
                bool fail = false;
                if (crossProcessor.TryPushChar(c))
                {
                    charsPushed++;
                    foreach (char s in suffix)
                    {
                        if (crossProcessor.TryPushChar(s))
                        {
                            charsPushed++;
                        }
                        else
                        {
                            fail = true;
                            break;
                        }
                    }

                    fail |= !crossProcessor.State.Satisfied;
                }
                else
                {
                    fail = true;
                }

                if (fail)
                {
                    for (uint i = 0; i < charsPushed; i++)
                    {
                        crossProcessor.PopChar();
                    }

                    return null;
                }
                else
                {
                    return new(0, 0);
                }
            }

            protected override void Undo()
            {
                for (uint i = 0; i <= suffix.Length; i++)
                {
                    crossProcessor.PopChar();
                }
            }
        }
    }
}
