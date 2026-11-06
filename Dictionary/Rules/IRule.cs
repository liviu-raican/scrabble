namespace Dictionary.Rules
{
    public interface IRule
    {
        RuleProcessor GetProcessor();
        RuleState? StartingState { get; }
    }
}
