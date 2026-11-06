using static Scrabble.Game.PlayedWord;

namespace Scrabble.Game
{
    public record PlayedWord(bool Horizontal, uint Row, uint Column, PlayedWordStone[] Stones, uint Score)
    {
        public string Word { get; } = Stones.Aggregate(string.Empty, (w, s) => $"{w}{(s.Stone.Joker ? $"({s.Stone.Letter})" : s.Stone.Letter.ToString())}");

        public override string ToString()
        {
            return (Horizontal ? $"{(char)('A' + Row)}{Column + 1}" : $"{Column + 1}{(char)('A' + Row)}") + $" {Word} {Score}";
        }

        public record BoardStone(char Letter, bool Joker = false);
        public record PlayedWordStone(BoardStone Stone, bool Played);
    }
}
