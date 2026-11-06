using static Scrabble.Game.PlayedWord;

namespace Scrabble.Game
{
    public class GameBoard(BoardStone?[,] stones)
    {
        public BoardStone? this[uint row, uint column] => stones[row, column];
    }
}
