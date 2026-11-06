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

        public uint Rows { get; } = (uint)squares.GetLength(0);
        public uint Columns { get; } = (uint)squares.GetLength(1);

        public Multiplier? this[uint row, uint column] => squares[row, column];
    }
}
