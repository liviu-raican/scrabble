namespace DAWGCompressor
{
    internal class LetterGroup
    {
        private LetterGroup? nextEntry;
        private LetterGroup? previousEntry;
        private LetterGroup head;
        private LetterGroup tail;

        internal LetterGroup? NextEntry
        {
            get => nextEntry;
            set
            {
                if (nextEntry != null)
                {
                    nextEntry.previousEntry = null;
                    nextEntry.Head = nextEntry;
                }

                nextEntry = value;

                if (nextEntry == null)
                {
                    Tail = this;
                }
                else
                {
                    nextEntry.previousEntry?.NextEntry = null;
                    nextEntry.previousEntry = this;
                    nextEntry.Head = head;
                    Tail = nextEntry.Tail;
                }
            }
        }
        internal LetterGroup? PreviousEntry
        {
            get => previousEntry;
            set
            {
                if (previousEntry != null)
                {
                    previousEntry.nextEntry = null;
                    previousEntry.Tail = previousEntry;
                }

                previousEntry = value;

                if (previousEntry == null)
                {
                    Head = this;
                }
                else
                {
                    previousEntry.nextEntry?.PreviousEntry = null;
                    previousEntry.nextEntry = this;
                    previousEntry.Tail = tail;
                    Head = previousEntry.Head;
                }
            }
        }
        internal LetterGroup Head
        {
            get => head;
            private set
            {
                head = value;
                nextEntry?.Head = value;
            }
        }
        internal LetterGroup Tail
        {
            get => tail;
            private set
            {
                tail = value;
                previousEntry?.Tail = value;
            }
        }

        internal Dictionary<char, LetterGroup> InternalLetters { get; }
        internal HashSet<char> Leaves { get; }

        internal LetterGroup(Dictionary<char, LetterGroup>? internalLetters = null, HashSet<char>? leaves = null)
        {
            head = this;
            tail = this;

            InternalLetters = internalLetters ?? [];
            Leaves = leaves ?? [];
        }
    }
}
