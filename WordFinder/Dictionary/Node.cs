namespace WordFinder.Dictionary
{
    internal class Node(char letter)
    {
        public char Letter { get; } = letter;

        public Node? NextSibling
        {
            get; set;
        }

        public Node? FirstChild
        {
            get; set;
        }
    }
}
