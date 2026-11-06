namespace Dictionary.Rules
{
    public abstract class RuleProcessor(RuleState startingState)
    {
        private readonly Stack<RuleState> StateHistory = new ([startingState]);

        public uint Length => (uint)StateHistory.Count - 1;

        public RuleState State => StateHistory.Peek();

        public bool TryPushChar(char c)
        {
            if (State.CanContinue)
            {
                RuleState? newState = AddChar(c);

                if (newState == null)
                    return false;

                StateHistory.Push(newState);

                return true;
            }

            return false;
        }

        public void PopChar()
        {
            if (Length > 0)
            {
                Undo();

                StateHistory.Pop();
            }
        }

        protected virtual void Undo()
        {
        }

        protected abstract RuleState? AddChar(char c);
    }
}
