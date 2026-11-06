using Dictionary.Rules;
using System.Collections;
using Dictionary.Rules.Impl;

namespace Dictionary
{
    public class DawgxDictionary
    {
        private readonly Node? root;

        public DawgxDictionary(byte[] dawgxBytes)
        {
            BitArray bits = new(dawgxBytes);

            int letterCount = GetIntInBits(0, 7);
            byte letterCountBitCount = (byte)Math.Ceiling(Math.Log2(letterCount));
            char firstLetter = (char)GetIntInBits(7, 16);
            int alphabetSpanBitCount = GetIntInBits(23, 4);

            char[] alphabet = new char[letterCount];
            for (int i = 0; i < letterCount; i++)
            {
                alphabet[i] = (char)(firstLetter + GetIntInBits(27 + i * alphabetSpanBitCount, alphabetSpanBitCount));
            }

            int nodeIndexBitCount = GetIntInBits(27 + letterCount * alphabetSpanBitCount, 5);
            int blockLength = 1 + letterCountBitCount + nodeIndexBitCount;
            int nodesBitStart = 32 + letterCount * alphabetSpanBitCount;

            Dictionary<int, Node> nodes = [];
            Node? previousSibling = null;
            int currentNodeIndex = 1;

            while (nodesBitStart + currentNodeIndex * blockLength < bits.Length)
            {
                Node currentNode = GetNodeAtIndex(currentNodeIndex);
                root ??= currentNode;

                previousSibling?.NextSibling = currentNode;
                int nextNodeIndex = GetIntInBits(nodesBitStart + (currentNodeIndex - 1) * blockLength + letterCountBitCount, nodeIndexBitCount);
                if (nextNodeIndex != 0)
                    currentNode.FirstChild = GetNodeAtIndex(nextNodeIndex);

                previousSibling = GetIntInBits(nodesBitStart + currentNodeIndex * blockLength - 1, 1) == 1 ? null : currentNode;

                currentNodeIndex++;
            }

            int GetIntInBits(int start, int length)
            {
                int result = 0;
                while (length-- > 0)
                {
                    result <<= 1;
                    result += bits[start++] ? 1 : 0;
                }

                return result;
            }

            Node GetNodeAtIndex(int index) // 1-based index
            {
                if (nodes.TryGetValue(index, out Node? node))
                {
                    return node;
                }

                Node newNode = new(alphabet[GetIntInBits(nodesBitStart + (index - 1) * blockLength, letterCountBitCount)]);
                nodes[index] = newNode;

                return newNode;
            }
        }

        public bool WordExists(string word)
        {
            return FindWords(new StringRule(word)).Any();
        }

        public IEnumerable<string> FindWords(IRule rule)
        {
            if (rule.StartingState == null)
            {
                yield break;
            }

            RuleProcessor processor = rule.GetProcessor();

            Traverser traverser = GetTraverser();
            Stack<char> currentWord = [];

            char? letter;
            bool moveIn = true;
            do
            {
                letter = moveIn ? traverser.MoveIn() : traverser.MoveSide();

                if (letter == null)
                {
                    if (currentWord.Count > 0)
                    {
                        if (!moveIn)
                        {
                            traverser.MoveBack();
                        }
                        currentWord.Pop();
                        processor.PopChar();

                        moveIn = false;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (processor.TryPushChar((char)letter))
                    {
                        currentWord.Push((char)letter);

                        if (traverser.IsWord && processor.State.Satisfied)
                        {
                            yield return new string([.. currentWord.Reverse()]);
                        }

                        if (processor.State.CanContinue)
                        {
                            moveIn = true;
                        }
                        else
                        {
                            currentWord.Pop();
                            processor.PopChar();
                            moveIn = false;
                        }
                    }
                    else
                    {
                        moveIn = false;
                    }
                }

            }
            while (true);
        }

        public Traverser GetTraverser()
        {
            return new Traverser(root);
        }
    }
}
