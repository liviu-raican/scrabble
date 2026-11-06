using Dictionary.Rules;
using Scrabble.Game;

namespace Scrabble.Rule
{
    public class FieldRule(PlayingBag bag, bool[] field) : IRule
    {
        public RuleState? StartingState { get; } = Processor.GetRuleState(field, 0, bag.TotalStones);

        public RuleProcessor GetProcessor() => new Processor(bag, field);

        private class Processor(PlayingBag bag, bool[] field) : RuleProcessor(GetRuleState(field, 0, bag.TotalStones))
        {
            internal static RuleState GetRuleState(bool[] field, uint lettersIn, uint remainingStones)
            {
                return new(0, uint.Min((uint)(field.Length - lettersIn), remainingStones));
            }

            private readonly PlayingBag playingBag = new(bag);
            private readonly Stack<char?> lettersUsed = [];

            protected override RuleState? AddChar(char c)
            {
                if (Length == field.Length)
                {
                    return null;
                }

                uint newStoneCount;

                if (field[Length])
                {
                    newStoneCount = playingBag.TotalStones;
                }
                else
                {
                    if (playingBag.GetStoneCount(c) > 0)
                    {
                        playingBag.ExtractStone(c);
                        lettersUsed.Push(c);
                    }
                    else
                    {
                        if (playingBag.GetStoneCount(null) > 0)
                        {
                            playingBag.ExtractStone(null);
                            lettersUsed.Push(null);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    newStoneCount = playingBag.TotalStones - 1;
                }
                
                RuleState? newState = GetRuleState(field, Length + 1, newStoneCount);

                return newState;
            }

            protected override void Undo()
            {
                if (!field[Length - 1])
                {
                    playingBag.InsertStone(lettersUsed.Pop());
                }
            }
        }
    }
}
