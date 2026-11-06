using Dictionary;
using Dictionary.Rules;
using Dictionary.Rules.Impl;
using Scrabble.Definition;
using Scrabble.Rule;
using System.Collections.Concurrent;
using System.Text;
using static Scrabble.Game.PlayedWord;

namespace Scrabble.Game
{
    public partial class ScrabbleGame
    {
        private readonly Coords center;
        private readonly PlayingBag playingBag;
        private readonly BoardStone?[,] stones;
        private readonly Atom[,] horizontalAtoms;
        private readonly Atom[,] verticalAtoms;
        private readonly WordSlot?[,] horizontalSlots;
        private readonly WordSlot?[,] verticalSlots;
        private readonly Stack<PlayedWord> playedWords;
        private readonly Dictionary<uint, Range> pendingHorizontalSlotUpdates;
        private readonly Dictionary<uint, Range> pendingVerticalSlotUpdates;

        public Board Board { get; }
        public LetterBag Bag { get; }
        public DawgxDictionary Dictionary { get; }
        public uint Score => (uint)playedWords.Sum(w => w.Score);
        public PlayedWord[] PlayedWords => [.. playedWords.Reverse()];
        public GameBoard GameBoard { get; }
        public PlayingBagView PlayingBagView { get; }

        public ScrabbleGame(Board board, LetterBag bag, DawgxDictionary dictionary)
        {
            Board = board;
            Bag = bag;            
            Dictionary = dictionary;

            playingBag = new(bag);
            PlayingBagView = new(playingBag);
            stones = new BoardStone[board.Rows, board.Columns];
            GameBoard = new(stones);

            center = new(board.Rows / 2, board.Columns / 2);

            horizontalAtoms = new Atom[board.Rows, board.Columns];
            verticalAtoms = new Atom[board.Rows, board.Columns];

            for (int i = 0; i < board.Rows; i++)
            {
                for (int j = 0; j < board.Columns; j++)
                {
                    horizontalAtoms[i, j] = new Atom(true, true);
                    verticalAtoms[j, i] = new Atom(true, true);
                }
            }

            horizontalSlots = new WordSlot?[board.Rows, board.Columns];
            verticalSlots = new WordSlot?[board.Rows, board.Columns];

            pendingHorizontalSlotUpdates = [];
            pendingVerticalSlotUpdates = [];

            TryReplaceAtom(true, center.Row, center.Column, new());
            TryReplaceAtom(false, center.Row, center.Column, new());

            UpdatePendingSlots();

            playedWords = [];
        }

        public PlayedWord PlayWord(bool horizontal, uint row, uint column, string word)
        {
            return PlayWord(horizontal, row, column,
                [.. PlayedWordRegex().Matches(word)
                    .Select(m =>
                    {
                        bool inParentheses = m.Value.StartsWith('(');
                        return new BoardStone(inParentheses ? m.Value[1] : m.Value[0], inParentheses);
                    })]
                    );
        }

        public PlayedWord PlayWord(bool horizontal, uint row, uint column, BoardStone[] word)
        {
            if (row >= Board.Rows || column >= Board.Columns)
            {
                throw new Exception("Coordinates not on board");
            }

            WordSlot slot = (horizontal ? horizontalSlots : verticalSlots)[row, column] ?? throw new Exception("Word cannot be played in that position");

            if (slot.WordRule.StartingState == null)
            {
                throw new Exception("Word cannot be played in that position");
            }

            RuleProcessor processor = slot.WordRule.GetProcessor();
            List<char?> newStones = [];
            Coords start = new(row, column);

            for (int i = 0; i < word.Length; i++)
            {
                BoardStone stone = word[i];

                if (!processor.TryPushChar(stone.Letter))
                {
                    throw new Exception("Word cannot be played in that position");
                }

                Coords coords = GetNewCoords(horizontal, row, column, i)!;
                BoardStone? currentStone = stones[coords.Row, coords.Column];

                if (currentStone == null)
                {
                    newStones.Add(stone.Joker ? null : stone.Letter);
                }
                else
                {
                    if (stone.Joker)
                    {
                        throw new Exception("Jokers can only be played on empty squares");
                    }
                }
            }

            if (!playingBag.HasStones(newStones))
            {
                throw new Exception("Not enough letters in the bag to play this word");
            }

            if (!Dictionary.WordExists(string.Concat(word.Select(s => s.Letter))))
            {
                throw new Exception("Word does not exist");
            }

            // play is valid

            // calculate the score
            uint score = CalculateScore(horizontal, row, column, word);

            // remove the stones from the bag
            foreach (char? letter in newStones)
            {
                playingBag.ExtractStone(letter);
            }

            // update the atoms and board
            Atom[,] atomLineChart = horizontal ? horizontalAtoms : verticalAtoms;
            Atom[,] atomCrossChart = horizontal ? verticalAtoms : horizontalAtoms;
            PlayedWordStone[] playedStones = new PlayedWordStone[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                Coords coords = GetNewCoords(horizontal, row, column, i)!;
                BoardStone? currentStone = stones[coords.Row, coords.Column];

                playedStones[i] = new(word[i], currentStone == null);

                if (currentStone == null)
                {
                    // place stone inside word
                    stones[coords.Row, coords.Column] = word[i];

                    // update atoms in word
                    TryReplaceAtom(horizontal, coords.Row, coords.Column, new(i == 0, i == word.Length - 1, false));
                    Atom currentCrossAtom = atomCrossChart[coords.Row, coords.Column];
                    TryReplaceAtom(!horizontal, coords.Row, coords.Column, new(currentCrossAtom.FreeBefore, currentCrossAtom.FreeAfter, false));

                    // update atoms coming across to word
                    Coords? crossCoords = GetNewCoords(!horizontal, coords.Row, coords.Column, -1);
                    if (crossCoords != null)
                    {
                        Atom preCrossAtom = atomCrossChart[crossCoords.Row, crossCoords.Column];
                        Atom preLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column];
                        if (preCrossAtom.Free)
                        {
                            TryReplaceAtom(!horizontal, crossCoords.Row, crossCoords.Column, preCrossAtom.Adjacent == null ? new(preCrossAtom.FreeBefore, false) : new(preCrossAtom.FreeBefore, false, preCrossAtom.Adjacent));
                        }
                        else
                        {
                            TryReplaceAtom(!horizontal, crossCoords.Row, crossCoords.Column, new(preCrossAtom.FreeBefore, false, false));
                            while (crossCoords != null && !(preLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column]).Free)
                            {
                                crossCoords = GetNewCoords(!horizontal, crossCoords.Row, crossCoords.Column, -1);
                            }
                        }

                        if (crossCoords != null)
                        {
                            TryReplaceAtom(horizontal, crossCoords.Row, crossCoords.Column, new(preLineAtom.FreeBefore, preLineAtom.FreeAfter, GetAdjacent(horizontal, crossCoords.Row, crossCoords.Column)!));
                        }
                    }

                    // update atoms going across from word
                    crossCoords = GetNewCoords(!horizontal, coords.Row, coords.Column, 1);
                    if (crossCoords != null)
                    {
                        Atom postCrossAtom = atomCrossChart[crossCoords.Row, crossCoords.Column];
                        Atom postLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column];
                        if (postCrossAtom.Free)
                        {
                            TryReplaceAtom(!horizontal, crossCoords.Row, crossCoords.Column, postCrossAtom.Adjacent == null ? new(false, postCrossAtom.FreeAfter) : new(false, postCrossAtom.FreeAfter, postCrossAtom.Adjacent));
                        }
                        else
                        {
                            TryReplaceAtom(!horizontal, crossCoords.Row, crossCoords.Column, new(false, postCrossAtom.FreeAfter, false));
                            while (crossCoords != null && !(postLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column]).Free)
                            {
                                crossCoords = GetNewCoords(!horizontal, crossCoords.Row, crossCoords.Column, 1);
                            }
                        }

                        if (crossCoords != null)
                        {
                            TryReplaceAtom(horizontal, crossCoords.Row, crossCoords.Column, new(postLineAtom.FreeBefore, postLineAtom.FreeAfter, GetAdjacent(horizontal, crossCoords.Row, crossCoords.Column)!));
                        }
                    }
                }
                else
                {
                    // update atom in word
                    TryReplaceAtom(horizontal, coords.Row, coords.Column, new(i == 0, i == word.Length - 1, false));
                }
            }

            // update atoms before word
            Coords? sideCoords = GetNewCoords(horizontal, row, column, -1);
            if (sideCoords != null)
            {
                Atom currentAtom = atomLineChart[sideCoords.Row, sideCoords.Column];
                Atom newAtom = currentAtom.Adjacent == null ? new(currentAtom.FreeBefore, false) : new(currentAtom.FreeBefore, false, currentAtom.Adjacent);
                TryReplaceAtom(horizontal, sideCoords.Row, sideCoords.Column, newAtom);

                currentAtom = atomCrossChart[sideCoords.Row, sideCoords.Column];
                newAtom = new(currentAtom.FreeBefore, currentAtom.FreeAfter, GetAdjacent(!horizontal, sideCoords.Row, sideCoords.Column)!);
                TryReplaceAtom(!horizontal, sideCoords.Row, sideCoords.Column, newAtom);
            }

            // update atoms after word
            sideCoords = GetNewCoords(horizontal, row, column, word.Length);
            if (sideCoords != null)
            {
                Atom currentAtom = atomLineChart[sideCoords.Row, sideCoords.Column];
                Atom newAtom = currentAtom.Adjacent == null ? new(false, currentAtom.FreeAfter) : new(false, currentAtom.FreeAfter, currentAtom.Adjacent);
                TryReplaceAtom(horizontal, sideCoords.Row, sideCoords.Column, newAtom);

                currentAtom = atomCrossChart[sideCoords.Row, sideCoords.Column];
                newAtom = new(currentAtom.FreeBefore, currentAtom.FreeAfter, GetAdjacent(!horizontal, sideCoords.Row, sideCoords.Column)!);
                TryReplaceAtom(!horizontal, sideCoords.Row, sideCoords.Column, newAtom);
            }

            // update slots
            UpdatePendingSlots();

            // Add the played word to the list
            PlayedWord playedWord = new(horizontal, row, column, playedStones, score);
            playedWords.Push(playedWord);

            return playedWord;
        }

        public bool Undo()
        {
            if (playedWords.Count == 0)
            {
                return false;
            }

            // remove the played word from the list
            PlayedWord word = playedWords.Pop();

            // remove the stones from the board
            for (int i = 0; i < word.Stones.Length; i++)
            {
                if (word.Stones[i].Played)
                {
                    Coords coords = GetNewCoords(word.Horizontal, word.Row, word.Column, i)!;
                    stones[coords.Row, coords.Column] = null;
                }
            }

            // update the atoms and board
            Atom[,] atomLineChart = word.Horizontal ? horizontalAtoms : verticalAtoms;
            Atom[,] atomCrossChart = word.Horizontal ? verticalAtoms : horizontalAtoms;

            for (int i = 0; i < word.Stones.Length; i++)
            {
                PlayedWordStone playedStone = word.Stones[i];
                Coords coords = GetNewCoords(word.Horizontal, word.Row, word.Column, i)!;

                if (playedStone.Played)
                {
                    // add stone to bag
                    playingBag.InsertStone(playedStone.Stone.Joker ? null : playedStone.Stone.Letter);

                    // update atoms in word
                    if (coords == center)
                    {
                        TryReplaceAtom(true, coords.Row, coords.Column, new());
                        TryReplaceAtom(false, coords.Row, coords.Column, new());
                    }
                    else
                    {
                        Adjacent? lineAdjacent = GetAdjacent(word.Horizontal, coords.Row, coords.Column);
                        bool freeBefore = i == 0 || word.Stones[i - 1].Played;
                        bool freeAfter = i == word.Stones.Length - 1 || word.Stones[i + 1].Played;
                        TryReplaceAtom(word.Horizontal, coords.Row, coords.Column, lineAdjacent == null ? new(freeBefore, freeAfter) : new(freeBefore, freeAfter, lineAdjacent));

                        Adjacent? crossAdjacent = GetAdjacent(!word.Horizontal, coords.Row, coords.Column);
                        Atom currentCrossAtom = atomCrossChart[coords.Row, coords.Column];
                        TryReplaceAtom(!word.Horizontal, coords.Row, coords.Column, crossAdjacent == null ? new(currentCrossAtom.FreeBefore, currentCrossAtom.FreeAfter) : new(currentCrossAtom.FreeBefore, currentCrossAtom.FreeAfter, crossAdjacent));
                    }

                    // update atoms coming across to word
                    Coords? crossCoords = GetNewCoords(!word.Horizontal, coords.Row, coords.Column, -1);
                    if (crossCoords != null)
                    {
                        Atom preCrossAtom = atomCrossChart[crossCoords.Row, crossCoords.Column];
                        Atom preLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column];
                        if (preCrossAtom.Free)
                        {
                            TryReplaceAtom(!word.Horizontal, crossCoords.Row, crossCoords.Column, preCrossAtom.Adjacent == null ? new(preCrossAtom.FreeBefore, true) : new(preCrossAtom.FreeBefore, true, preCrossAtom.Adjacent));
                        }
                        else
                        {
                            TryReplaceAtom(!word.Horizontal, crossCoords.Row, crossCoords.Column, new(preCrossAtom.FreeBefore, true, false));
                            while (crossCoords != null && !(preLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column]).Free)
                            {
                                crossCoords = GetNewCoords(!word.Horizontal, crossCoords.Row, crossCoords.Column, -1);
                            }
                        }
                        if (crossCoords != null)
                        {
                            Adjacent? crossAdjacent = GetAdjacent(word.Horizontal, crossCoords.Row, crossCoords.Column);
                            TryReplaceAtom(word.Horizontal, crossCoords.Row, crossCoords.Column,
                                crossAdjacent == null ?
                                new(preLineAtom.FreeBefore, preLineAtom.FreeAfter) :
                                new(preLineAtom.FreeBefore, preLineAtom.FreeAfter, crossAdjacent));
                        }
                    }

                    // update atoms going across from word
                    crossCoords = GetNewCoords(!word.Horizontal, coords.Row, coords.Column, 1);
                    if (crossCoords != null)
                    {
                        Atom postCrossAtom = atomCrossChart[crossCoords.Row, crossCoords.Column];
                        Atom postLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column];
                        if (postCrossAtom.Free)
                        {
                            TryReplaceAtom(!word.Horizontal, crossCoords.Row, crossCoords.Column, postCrossAtom.Adjacent == null ? new(true, postCrossAtom.FreeAfter) : new(true, postCrossAtom.FreeAfter, postCrossAtom.Adjacent));
                        }
                        else
                        {
                            TryReplaceAtom(!word.Horizontal, crossCoords.Row, crossCoords.Column, new(true, postCrossAtom.FreeAfter, false));
                            while (crossCoords != null && !(postLineAtom = atomLineChart[crossCoords.Row, crossCoords.Column]).Free)
                            {
                                crossCoords = GetNewCoords(!word.Horizontal, crossCoords.Row, crossCoords.Column, 1);
                            }
                        }
                        if (crossCoords != null)
                        {
                            Adjacent? crossAdjacent = GetAdjacent(word.Horizontal, crossCoords.Row, crossCoords.Column);
                            TryReplaceAtom(word.Horizontal, crossCoords.Row, crossCoords.Column,
                                crossAdjacent == null ?
                                new(postLineAtom.FreeBefore, postLineAtom.FreeAfter) :
                                new(postLineAtom.FreeBefore, postLineAtom.FreeAfter, crossAdjacent));
                        }
                    }
                }
                else
                {
                    // update atom in word
                    bool freeBefore = i == 0 || word.Stones[i - 1].Played;
                    bool freeAfter = i == word.Stones.Length - 1 || word.Stones[i + 1].Played;
                    TryReplaceAtom(word.Horizontal, coords.Row, coords.Column, new(freeBefore, freeAfter, false));
                }
            }

            // update atoms before word
            Coords? sideCoords = GetNewCoords(word.Horizontal, word.Row, word.Column, -1);
            if (sideCoords != null)
            {
                Atom currentAtom = atomLineChart[sideCoords.Row, sideCoords.Column];
                TryReplaceAtom(word.Horizontal, sideCoords.Row, sideCoords.Column, currentAtom.Adjacent == null ? new(currentAtom.FreeBefore, word.Stones[0].Played) : new(currentAtom.FreeBefore, word.Stones[0].Played, currentAtom.Adjacent));

                currentAtom = atomCrossChart[sideCoords.Row, sideCoords.Column];
                Adjacent? sideAdjacent = GetAdjacent(!word.Horizontal, sideCoords.Row, sideCoords.Column);
                TryReplaceAtom(!word.Horizontal, sideCoords.Row, sideCoords.Column, sideAdjacent == null ? new(currentAtom.FreeBefore, currentAtom.FreeAfter) : new(currentAtom.FreeBefore, currentAtom.FreeAfter, sideAdjacent));
            }

            // update atoms after word
            sideCoords = GetNewCoords(word.Horizontal, word.Row, word.Column, word.Stones.Length);
            if (sideCoords != null)
            {
                Atom currentAtom = atomLineChart[sideCoords.Row, sideCoords.Column];
                TryReplaceAtom(word.Horizontal, sideCoords.Row, sideCoords.Column, currentAtom.Adjacent == null ? new(word.Stones[^1].Played, currentAtom.FreeAfter) : new(word.Stones[^1].Played, currentAtom.FreeAfter, currentAtom.Adjacent));

                currentAtom = atomCrossChart[sideCoords.Row, sideCoords.Column];
                Adjacent? sideAdjacent = GetAdjacent(!word.Horizontal, sideCoords.Row, sideCoords.Column);
                TryReplaceAtom(!word.Horizontal, sideCoords.Row, sideCoords.Column, sideAdjacent == null ? new(currentAtom.FreeBefore, currentAtom.FreeAfter) : new(currentAtom.FreeBefore, currentAtom.FreeAfter, sideAdjacent));
            }

            // update slots
            UpdatePendingSlots();

            return true;
        }

        public IEnumerable<PlayedWord> FindPlays(IEnumerable<char?> bagStones, IRule? condition = null)
        {
            if (!playingBag.HasStones(bagStones))
            {
                throw new Exception("These letters are not available in the bag");
            }

            BlockingCollection<PlayedWord> results = [];
            List<Task> positionalSearches = [];

            for (uint row = 0; row < Board.Rows; row++)
            {
                for (uint column = 0; column < Board.Columns; column++)
                {
                    foreach (bool horizontal in new bool[] { true, false })
                    {
                        IRule? slotRule;
                        WordSlot? slot = horizontal ? horizontalSlots[row, column] : verticalSlots[row, column];

                        if (slot != null)
                        {
                            IRule rule = condition == null ?
                            new AllRule(
                                slotRule = slot.WordRule,
                                new FieldRule(new(Bag, bagStones), slot.Field)
                                ) :
                            new AllRule(
                                slotRule = slot.WordRule,
                                new FieldRule(new(Bag, bagStones), slot.Field),
                                condition
                                );

                            uint searchRow = row;
                            uint searchColumn = column;
                            bool searchHorizontal = horizontal;

                            positionalSearches.Add(Task.Run(
                                () =>
                                {
                                    foreach (string word in Dictionary.FindWords(rule))
                                    {
                                        PlayedWordStone[] playedStones = [.. word.Select(
                                            (l, i) =>
                                            {
                                                Coords coords = GetNewCoords(searchHorizontal, searchRow, searchColumn, i)!;
                                                BoardStone? existing = stones[coords.Row, coords.Column];
                                                if (existing == null)
                                                {
                                                    return new PlayedWordStone(new BoardStone(l), true);
                                                }
                                                else
                                                {
                                                    return new PlayedWordStone(existing, false);
                                                }
                                            }
                                            )];

                                        BoardStone[] wordStones = [.. playedStones.Select(p => p.Stone)];

                                        results.Add(new PlayedWord(searchHorizontal, searchRow, searchColumn, playedStones, CalculateScore(searchHorizontal, searchRow, searchColumn, wordStones)));
                                    }
                                }
                                ));
                        }
                    }
                }
            }

            Task.WhenAll(positionalSearches).ContinueWith(_ => results.CompleteAdding(), TaskScheduler.Default);

            foreach (PlayedWord playedWord in results.GetConsumingEnumerable())
            {
                yield return playedWord;
            }
        }

        private uint CalculateScore(bool horizontal, uint row, uint column, BoardStone[] word)
        {
            uint wordScore = 0;
            uint wordMultiplicationFactor = 1;
            uint adjacentsScore = 0;
            uint newLetters = 0;

            Atom[,] atomChart = horizontal ? horizontalAtoms : verticalAtoms;

            for (int i = 0; i < word.Length; i++)
            {
                Coords coords = GetNewCoords(horizontal, row, column, i)!;
                BoardStone wordStone = word[i];

                uint value = wordStone.Joker ? 0 : Bag.Letters[wordStone.Letter].Value;

                if (stones[coords.Row, coords.Column] == null)
                {
                    Multiplier? multiplier = Board[coords.Row, coords.Column];
                    uint letterFactor = multiplier?.Type == MultiplierType.Letter ? multiplier.Factor : 1;
                    uint wordFactor = multiplier?.Type == MultiplierType.Word ? multiplier.Factor : 1;

                    Adjacent? adjacent = atomChart[coords.Row, coords.Column].Adjacent;
                    if (adjacent != null)
                    {
                        adjacentsScore += (adjacent.AddedValue + letterFactor * value) * wordFactor;
                    }

                    wordScore += letterFactor * value;
                    wordMultiplicationFactor *= wordFactor;

                    newLetters++;
                }
                else
                {
                    wordScore += value;
                }
            }

            return wordScore * wordMultiplicationFactor + adjacentsScore + (uint)(newLetters == 7 ? 50 : 0);
        }

        private Adjacent? GetAdjacent(bool horizontal, uint row, uint column)
        {
            BoardStone? currentStone = stones[row, column];

            if (currentStone != null)
            {
                return null;
            }

            BoardStone? crossStone;
            uint addedValue = 0;
            StringBuilder halfWord = new(horizontal ? (int)Board.Rows : (int)Board.Columns);
            Coords? crossCoords = GetNewCoords(!horizontal, row, column, -1);

            while (crossCoords != null)
            {
                crossStone = stones[crossCoords.Row, crossCoords.Column];
                if (crossStone == null)
                {
                    break;
                }

                halfWord.Insert(0, crossStone.Letter);
                addedValue += crossStone.Joker ? 0 : Bag.Letters[crossStone.Letter].Value;

                crossCoords = GetNewCoords(!horizontal, crossCoords.Row, crossCoords.Column, -1);
            }
            string prefix = halfWord.ToString();

            halfWord.Clear();
            crossCoords = GetNewCoords(!horizontal, row, column, 1);

            while (crossCoords != null)
            {
                crossStone = stones[crossCoords.Row, crossCoords.Column];
                if (crossStone == null)
                {
                    break;
                }

                halfWord.Append(crossStone.Letter);
                addedValue += crossStone.Joker ? 0 : Bag.Letters[crossStone.Letter].Value;

                crossCoords = GetNewCoords(!horizontal, crossCoords.Row, crossCoords.Column, 1);
            }
            string suffix = halfWord.ToString();

            if (prefix == string.Empty && suffix == string.Empty)
            {
                return null;
            }
            else
            {
                var adjacentProcessor = new CrosswordRule(prefix, suffix, Dictionary).GetProcessor();
                HashSet<char> allowedLetters = [.. Bag.Letters.Keys.Where(
                                    c =>
                                    {
                                        if (adjacentProcessor.TryPushChar(c))
                                        {
                                            adjacentProcessor.PopChar();
                                            return true;
                                        }
                                        return false;
                                    }
                                    )];

                return new Adjacent(allowedLetters, addedValue);
            }
        }

        private void TryReplaceAtom(bool horizontal, uint row, uint column, Atom newAtom)
        {
            Atom[,] atomChart = horizontal ? horizontalAtoms : verticalAtoms;
            Atom currentAtom = atomChart[row, column];

            if (newAtom.IsConnected != currentAtom.IsConnected ||
                newAtom.FreeAfter != currentAtom.FreeAfter ||
                newAtom.FreeBefore != currentAtom.FreeBefore ||
                newAtom.Free != currentAtom.Free ||
                newAtom.Adjacent == null && currentAtom.Adjacent != null ||
                newAtom.Adjacent != null && currentAtom.Adjacent == null ||
                currentAtom.Adjacent != newAtom.Adjacent && !currentAtom.Adjacent!.AllowedLetters.SetEquals(newAtom.Adjacent!.AllowedLetters)
                )
            {
                atomChart[row, column] = newAtom;

                EnqueuePendingSlotUpdates(horizontal, row, column);
            }
        }

        private void EnqueuePendingSlotUpdates(bool horizontal, uint row, uint column)
        {
            WordSlot?[,] slotChart = horizontal ? horizontalSlots : verticalSlots;
            Coords? sideCoords = new(row, column);
            uint distance = 0;
            while (sideCoords != null)
            {
                if (slotChart[sideCoords.Row, sideCoords.Column]?.Field.Length < ++distance)
                {
                    break;
                }

                sideCoords = GetNewCoords(horizontal, row, column, (int)-distance);
            }

            Dictionary<uint, Range> slotUpdates = horizontal ? pendingHorizontalSlotUpdates : pendingVerticalSlotUpdates;
            uint anchor = horizontal ? column : row;
            uint line = horizontal ? row : column;
            if (slotUpdates.TryGetValue(line, out Range? range))
            {
                uint from = uint.Min(anchor - (distance - 1), range.From);
                slotUpdates[line] = new(from, uint.Max(anchor, range.To) - from);
            }
            else
            {
                slotUpdates[line] = new(anchor - (distance - 1), distance - 1);
            }
        }

        private void UpdatePendingSlots()
        {
            UpdatePendingSlots(true);
            UpdatePendingSlots(false);
        }

        private void UpdatePendingSlots(bool horizontal)
        {
            var pendingDictionary = horizontal ? pendingHorizontalSlotUpdates : pendingVerticalSlotUpdates;
            
            foreach (var entry in pendingDictionary)
            {
                for (uint index = entry.Value.From; index <= entry.Value.To; index++)
                {
                    UpdateSlot(horizontal, horizontal ? entry.Key : index, horizontal ? index : entry.Key);
                }
            }
            pendingDictionary.Clear();
            
        }

        private void UpdateSlot(bool horizontal, uint row, uint column)
        {
            WordSlot?[,] slotChart = horizontal ? horizontalSlots : verticalSlots;
            Atom[,] atomChart = horizontal ? horizontalAtoms : verticalAtoms;

            Coords? coords = new(row, column);
            Atom atom;
            uint lettersUsed = 0;
            bool letterUsed = false;
            Stack<List<IRule>> ruleGroups = [];
            List<IRule> ruleSpan = [];
            StringBuilder blockBuilder = new();
            uint freeSquares = 0;
            bool connected = false;
            uint index = 0;
            List<bool> field = [];
            uint maxLength = 0;

            do
            {
                atom = atomChart[coords.Row, coords.Column];

                if (index == 0 && !atom.FreeBefore)
                {
                    slotChart[row, column] = null;
                    return;
                }

                if (atom.Free)
                {
                    if (++lettersUsed > 7)
                    {
                        break;
                    }
                    
                    if (atom.Adjacent == null)
                    {
                        if (ruleGroups.Count == 0)
                        {
                            freeSquares++;

                            if (!atom.FreeAfter)
                            {
                                ruleSpan.Add(freeSquares > 1 ? new LengthRule(freeSquares) : new CharRule());
                                freeSquares = 0;
                            }
                        }
                        else
                        {
                            ruleSpan.Add(new CharRule());
                        }
                    }
                    else
                    {
                        if (freeSquares > 0)
                        {
                            ruleSpan.Add(freeSquares > 1 ? new LengthRule(freeSquares) : new CharRule());
                            freeSquares = 0;
                        }
                        ruleSpan.Add(new SetRule(atom.Adjacent.AllowedLetters));
                    }

                    letterUsed = true;
                    field.Add(false);
                }
                else
                {
                    blockBuilder.Append(stones[coords.Row, coords.Column]!.Letter);

                    if (atom.FreeAfter)
                    {
                        ruleSpan.Add(new StringRule(blockBuilder.ToString()));
                        blockBuilder.Clear();
                    }

                    field.Add(true);
                }

                bool addRuleGroup = false;
                if (ruleGroups.Count == 0)
                {
                    connected |= atom.IsConnected;

                    if (index > 0 && connected && letterUsed && atom.FreeAfter)
                    {
                        if (freeSquares > 0)
                        {
                            ruleSpan.Add(freeSquares > 1 ? new LengthRule(freeSquares) : new CharRule());
                        }

                        addRuleGroup = true;
                    }
                }
                else
                {
                    if (letterUsed && atom.FreeAfter)
                    {
                        addRuleGroup = true;
                    }
                }

                if (addRuleGroup)
                {
                    ruleGroups.Push(ruleSpan);
                    ruleSpan = [];
                    letterUsed = false;
                    maxLength = index + 1;
                }

                coords = GetNewCoords(horizontal, coords.Row, coords.Column, 1);
                index++;
            }
            while (coords != null);

            if (ruleGroups.Count == 0)
            {
                slotChart[row, column] = null;
            }
            else
            {
                slotChart[row, column] = new(ruleGroups.Aggregate<List<IRule>, IRule?>(null, (accum, r) => accum is null ? (r.Count == 1 ? r[0] : new SequenceRule(r)) : new SequenceRule(r.Append(new OptionalRule(accum))))!, [.. field.Take((int)maxLength)]);
            }
        }

        private Coords? GetNewCoords(bool horizontal, uint row, uint column, int by)
        {
            if (horizontal)
            {
                long to = column + by;
                if (to >= Board.Columns || to < 0)
                {
                    return null;
                }

                return new(row, (uint)to);
            }
            else
            {
                long to = row + by;
                if (to >= Board.Rows || to < 0)
                {
                    return null;
                }

                return new((uint)to, column);
            }
        }
    }
}
