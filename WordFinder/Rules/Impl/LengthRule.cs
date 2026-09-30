namespace WordFinder.Rules.Impl
{
    public class LengthRule(uint minLength, uint range = 0) : IRule
    {
        public RuleState? StartingState { get; } = new(minLength, range);

        public RuleProcessor GetProcessor()
        {
            return new Processor(minLength, range);
        }

        private class Processor(uint minLength, uint range) : RuleProcessor(new(minLength, range))
        {
            private readonly uint minLength = minLength;
            private readonly uint maxLength = minLength + range;

            protected override RuleState? AddChar(char c)
            {
                uint nextLength = (uint)Length + 1;
                return maxLength < nextLength ? null : new(minLength > nextLength ? minLength - nextLength : 0, minLength < nextLength ? range - nextLength + minLength : range);
            }
        }
    }
}
