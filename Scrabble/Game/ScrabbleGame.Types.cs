using Dictionary.Rules;
using System.Text.RegularExpressions;

namespace Scrabble.Game
{
    partial class ScrabbleGame
	{
		private record Adjacent(HashSet<char> AllowedLetters, uint AddedValue);

		private record WordSlot(IRule WordRule, bool[] Field);

		private class Atom
		{
			private readonly bool isCenter;

			internal bool FreeBefore { get; init; }
			internal bool FreeAfter { get; init; }
			internal bool Free { get; init; }
			internal Adjacent? Adjacent { get; init; }
			internal bool IsConnected => !FreeBefore || !FreeAfter || !Free || Adjacent?.AllowedLetters != null || isCenter;

			internal Atom(bool freeBefore, bool freeAfter, bool free = true)
			{
				FreeBefore = freeBefore;
				FreeAfter = freeAfter;
				Free = free;
			}

			internal Atom(bool freeBefore, bool freeAfter, Adjacent adjacent) : this(freeBefore, freeAfter)
			{
				Adjacent = adjacent;
			}

			internal Atom() : this(true, true)
			{
				isCenter = true;
			}
		}

		private record Coords(uint Row, uint Column);

		private record Range(uint From, uint Span)
		{
			internal uint To { get; } = From + Span;
        }

        [GeneratedRegex(@"\([^)]+\)|.")]
        private static partial Regex PlayedWordRegex();
    }
}
