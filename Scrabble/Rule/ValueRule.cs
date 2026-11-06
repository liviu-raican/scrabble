using Dictionary.Rules;
using Scrabble.Game;
using System.Collections.Immutable;

namespace Scrabble.Rule
{
    public class ValueRule(PlayingBag bag, uint minValue = 0, uint valueRange = ValueRule.Rangeless) : IRule
    {
        public const uint Rangeless = 1000000;

        public RuleState? StartingState { get; } = Processor.GetRuleState(minValue, valueRange, 0, bag.StoneCountByValue);

        public RuleProcessor GetProcessor()
        {
            return new Processor(bag, minValue, valueRange);
        }

        private class Processor(PlayingBag bag, uint minValue, uint valueRange) : RuleProcessor(GetRuleState(minValue, valueRange, 0, bag.StoneCountByValue) ?? throw new ArgumentException("Letter bag cannot fulfill value range"))
        {
            private static readonly EqualityComparer<PlayingBagEntry> playingBagEntryContentComparer = EqualityComparer<PlayingBagEntry>.Create(
                (e1, e2) =>
                {
                    if (e1 == e2)
                    {
                        return true;
                    }

                    if (e1 is null || e2 is null || e1.Bag.TotalStones != e2.Bag.TotalStones || e1.Bag.TotalValue != e2.Bag.TotalValue || e1.Bag.FullBag != e2.Bag.FullBag || e1.Bag.GetStoneCount(null) != e2.Bag.GetStoneCount(null))
                    {
                        return false;
                    }

                    return e1.Bag.FullBag.Letters.All(entry => e1.Bag.GetStoneCount(entry.Key) == e2.Bag.GetStoneCount(entry.Key));
                }
                );

            private static Stack<List<PlayingBagEntry>> InitializePlayingBagHistory(PlayingBag bag)
            {
                Stack<List<PlayingBagEntry>> history = [];

                List<PlayingBagEntry> first = [];
                first.Add(new PlayingBagEntry(new(bag), null, true));
                history.Push(first);

                return history;
            }

            internal static RuleState? GetRuleState(uint minValue, uint valueRange, uint currentValue, ImmutableSortedDictionary<uint, uint> stoneCountByValue)
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
                    foreach (var kvp in stoneCountByValue.Reverse())
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
                foreach (var kvp in stoneCountByValue)
                {
                    if (kvp.Key == 0)
                    {
                        maxChars += kvp.Value;
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

            private readonly Stack<List<PlayingBagEntry>> playingBagsHistory = InitializePlayingBagHistory(bag);

            protected override RuleState? AddChar(char c)
            {
                RuleState? combinedState = null;

                List<PlayingBagEntry> bagSet = playingBagsHistory.Peek();
                List<PlayingBagEntry> newSet = [];
                foreach (PlayingBagEntry entry in bagSet)
                {
                    RuleState? letterState;
                    bool addedToSet = false;
                    if (entry.Bag.GetStoneCount(c) > 0)
                    {
                        entry.Bag.ExtractStone(c);
                        letterState = GetRuleState(minValue, valueRange, bag.TotalValue - entry.Bag.TotalValue, entry.Bag.StoneCountByValue);

                        if (letterState == null || newSet.Contains(entry, playingBagEntryContentComparer))
                        {
                            entry.Bag.InsertStone(c);
                        }
                        else
                        {
                            combineState(letterState);
                            newSet.Add(new(entry.Bag, c, false));
                            addedToSet = true;
                        }
                    }
                    else
                    {
                        letterState = null;
                    }

                    if (entry.Bag.GetStoneCount(null) > 0)
                    {
                        RuleState? jokerState;
                        if (letterState == null)
                        {
                            entry.Bag.ExtractStone(null);
                            jokerState = GetRuleState(minValue, valueRange, bag.TotalValue - entry.Bag.TotalValue, entry.Bag.StoneCountByValue);

                            if (jokerState == null || newSet.Contains(entry, playingBagEntryContentComparer))
                            {
                                entry.Bag.InsertStone(null);
                            }
                            else
                            {
                                combineState(jokerState);
                                newSet.Add(new(entry.Bag, null, false));
                            }
                        }
                        else
                        {
                            PlayingBag cloneBag = new(entry.Bag);
                            if (addedToSet)
                            {
                                cloneBag.InsertStone(c);
                            }

                            cloneBag.ExtractStone(null);
                            jokerState = GetRuleState(minValue, valueRange, bag.TotalValue - cloneBag.TotalValue, cloneBag.StoneCountByValue);

                            if (jokerState != null)
                            {
                                PlayingBagEntry newEntry = new(cloneBag, null, true);
                                if (!newSet.Contains(newEntry, playingBagEntryContentComparer))
                                {
                                    combineState(jokerState);
                                    newSet.Add(newEntry);
                                }
                            }
                        }
                    }
                }

                if (combinedState != null)
                {
                    playingBagsHistory.Push(newSet);
                }

                return combinedState;

                void combineState(RuleState newState)
                {
                    if (combinedState == null)
                    {
                        combinedState = newState;
                    }
                    else
                    {
                        uint minChars = uint.Min(newState.MinChars, combinedState.MinChars);
                        combinedState = new(minChars, uint.Max(newState.MaxChars, combinedState.MaxChars) - minChars);
                    }
                }
            }

            protected override void Undo()
            {
                foreach (var playingBagEntry in playingBagsHistory.Pop())
                {
                    if (!playingBagEntry.Origin)
                        playingBagEntry.Bag.InsertStone(playingBagEntry.RemovedStone);
                }
            }

            private record PlayingBagEntry(PlayingBag Bag, char? RemovedStone, bool Origin);
        }
    }
}
