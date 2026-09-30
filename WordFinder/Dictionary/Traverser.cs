namespace WordFinder.Dictionary
{
    public class Traverser
    {
        private readonly Node? root;
        private readonly Stack<Node> nodeStack = [];
        private readonly Dictionary<uint, Stack<Node>> snapshots = [];

        private uint snapshotIndex;

        public bool IsWord => nodeStack.Count > 0 && nodeStack.Peek().FirstChild == null;

        internal Traverser(Node? root)
        {
            this.root = root;
        }

        public char? MoveIn()
        {
            Node? inNode;

            if (nodeStack.Count == 0)
            {
                inNode = root;
            }
            else
            {
                inNode = nodeStack.Peek().FirstChild;
            }

            if (inNode != null)
            {
                nodeStack.Push(inNode);

                return inNode.Letter;
            }

            return null;
        }

        public char? MoveSide()
        {
            if (nodeStack.Count == 0)
                return null;

            Node? sideNode = nodeStack.Peek().NextSibling;
            if (sideNode != null)
            {
                nodeStack.Pop();
                nodeStack.Push(sideNode);

                return sideNode.Letter;
            }

            return null;
        }

        public bool MoveBack()
        {
            if (nodeStack.Count == 0)
                return false;

            nodeStack.Pop();

            return true;
        }

        public (bool FoundTransit, bool FoundWord) MoveToLetter(char c)
        {
            char? next = MoveIn();

            if (next == null)
            {
                return (false, false);
            }

            int hits = 0;
            Node? moveNode = null;
            bool foundWord = false;
            bool foundTransit = false;
            while (next != null && hits < 2)
            {
                if (next == c)
                {
                    hits++;

                    if (IsWord)
                    {
                        foundWord = true;
                        moveNode ??= nodeStack.Peek();
                    }
                    else
                    {
                        foundTransit = true;
                        moveNode = nodeStack.Peek();
                    }
                }

                next = MoveSide();
            }

            if (moveNode == null)
            {
                MoveBack();
            }
            else
            {
                if (nodeStack.Peek() != moveNode)
                {
                    nodeStack.Pop();
                    nodeStack.Push(moveNode);
                }
            }

            return (FoundTransit: foundTransit, FoundWord: foundWord);
        }

        public uint SaveSnapshot()
        {
            uint newIndex = snapshotIndex++;
            snapshots[newIndex] = new (nodeStack);
            return newIndex;
        }

        public void RestoreSnapshot(uint index)
        {
            if (snapshots.TryGetValue(index, out Stack<Node>? reversedNodes))
            {
                nodeStack.Clear();
                foreach (var node in reversedNodes)
                {
                    nodeStack.Push(node);
                }
            }
        }

        public void DiscardSnapshot(uint index)
        {
            snapshots.Remove(index);
        }

        public Traverser Split()
        {
            return new Traverser(nodeStack.Count > 0 ? nodeStack.Peek() : root);
        }
    }
}
