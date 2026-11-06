namespace Scrabble.Game
{
    public class PlayingBagView(PlayingBag playingBag)
    {
        public uint this[char? letter] => playingBag.GetStoneCount(letter);
    }
}
