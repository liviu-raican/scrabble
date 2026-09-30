namespace Scrabble.Definition
{
    public class Board(Multiplier?[,] squares)
    {
        public static readonly Board Classic = new(
            new Multiplier?[,]
            {
                { Multiplier.Red, null, null, Multiplier.LightBlue, null, null, null, Multiplier.Red, null, null, null, Multiplier.LightBlue, null, null, Multiplier.Red},
                { null, Multiplier.Pink, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Pink, null },
                { null, null, Multiplier.Pink, null, null, null, Multiplier.LightBlue, null, Multiplier.LightBlue, null, null, null, Multiplier.Pink, null, null },
                { Multiplier.LightBlue, null, null, Multiplier.Pink, null, null, null, Multiplier.LightBlue, null, null, null, Multiplier.Pink, null, null, Multiplier.LightBlue },
                { null, null, null, null, Multiplier.Pink, null, null, null, null, null, Multiplier.Pink, null, null, null, null },
                { null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null },
                { null, null, Multiplier.LightBlue, null, null, null, Multiplier.LightBlue, null, Multiplier.LightBlue, null, null, null, Multiplier.LightBlue, null, null },
                { Multiplier.Red, null, null, Multiplier.LightBlue, null, null, null, Multiplier.Pink, null, null, null, Multiplier.LightBlue, null, null, Multiplier.Red },
                { null, null, Multiplier.LightBlue, null, null, null, Multiplier.LightBlue, null, Multiplier.LightBlue, null, null, null, Multiplier.LightBlue, null, null },
                { null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null },
                { null, null, null, null, Multiplier.Pink, null, null, null, null, null, Multiplier.Pink, null, null, null, null },
                { Multiplier.LightBlue, null, null, Multiplier.Pink, null, null, null, Multiplier.LightBlue, null, null, null, Multiplier.Pink, null, null, Multiplier.LightBlue },
                { null, null, Multiplier.Pink, null, null, null, Multiplier.LightBlue, null, Multiplier.LightBlue, null, null, null, Multiplier.Pink, null, null },
                { null, Multiplier.Pink, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Blue, null, null, null, Multiplier.Pink, null },
                { Multiplier.Red, null, null, Multiplier.LightBlue, null, null, null, Multiplier.Red, null, null, null, Multiplier.LightBlue, null, null, Multiplier.Red}
            }
            );

        private readonly Multiplier?[,] squares = (Multiplier?[,])squares.Clone();

        public int Rows { get; } = squares.GetLength(0);
        public int Columns { get; } = squares.GetLength(1);

        public Multiplier? this[int row, int column] => squares[row, column];
    }
}
