namespace WordFinder.Rules
{
    public record RuleState(uint MinChars, uint MaxRange = RuleState.Rangeless)
    {
        public const uint Rangeless = 1000000;

        public bool CanContinue => !Satisfied || MaxRange > 0;
        public bool Satisfied => MinChars == 0;
        public uint MaxChars => MinChars + MaxRange;
    }
}
