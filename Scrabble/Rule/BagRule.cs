using Scrabble.Definition;
using Scrabble.Game;
using System.Collections.Immutable;
using WordFinder.Rules;

namespace Scrabble.Rule
{
    public class BagRule(LetterBag bag, uint minValue = 0, uint valueRange = BagRule.Rangeless) : IRule
    {
        public const uint Rangeless = 1000000;

        public RuleState? StartingState { get; } = Processor.GetRuleState(bag.StoneCountByValue, minValue, valueRange, 0);

        public RuleProcessor GetProcessor()
        {
            return new Processor(bag, minValue, valueRange);
        }

        internal class Processor(LetterBag bag, uint minValue, uint valueRange) : RuleProcessor(GetRuleState(bag.StoneCountByValue, minValue, valueRange, 0) ?? throw new ArgumentException("Letter bag cannot fulfill value range"))
        {
            internal static RuleState? GetRuleState(ImmutableSortedDictionary<uint, uint> bagStoneCountByValue, uint minValue, uint valueRange, uint currentValue)
            {
                uint maxValue = minValue + valueRange;
                if (currentValue > maxValue)
                {
                    return null;
                }

                uint goingValue;
                uint minChars = 0;
                if (currentValue < minValue)
                {
                    goingValue = currentValue;
                    foreach (var kvp in bagStoneCountByValue.Reverse())
                    {
                        if (kvp.Key == 0)
                        {
                            break;
                        }

                        if (currentValue + kvp.Key > maxValue)
                        {
                            continue;
                        }

                        uint needed = (uint)Math.Ceiling((minValue - goingValue) / (double)kvp.Key);
                        if (needed > kvp.Value)
                        {
                            goingValue += kvp.Key * kvp.Value;
                            minChars += kvp.Value;
                        }
                        else
                        {
                            goingValue += kvp.Key * needed;
                            minChars += needed;
                            break;
                        }
                    }

                    if (goingValue < minValue)
                    {
                        return null;
                    }
                }

                uint maxChars = 0;
                goingValue = currentValue;
                foreach (var kvp in bagStoneCountByValue)
                {
                    if (kvp.Key == 0)
                    {
                        continue;
                    }

                    uint fitting = (uint)Math.Floor((maxValue - goingValue) / (double)kvp.Key);
                    if (fitting > kvp.Value)
                    {
                        goingValue += kvp.Key * kvp.Value;
                        maxChars += kvp.Value;
                    }
                    else
                    {
                        maxChars += fitting;
                        break;
                    }
                }

                return new(minChars, maxChars - minChars);
            }

            private readonly PlayingBag playingBag = new(bag);

            protected override RuleState? AddChar(char c)
            {
                if (!playingBag.ExtractLetter(c) && !playingBag.ExtractLetter(null))
                {
                    return null;
                }

                RuleState? newState = GetRuleState(playingBag.StoneCountByValue, minValue, valueRange, bag.TotalValue - playingBag.TotalValue);
                if (newState == null)
                {
                    playingBag.ReturnStone();
                }

                return newState;
            }

            protected override void Undo()
            {
                playingBag.ReturnStone();
            }
        }
    }
}
