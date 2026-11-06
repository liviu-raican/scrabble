namespace Scrabble.Definition
{
    public record Multiplier(MultiplierType Type, uint Factor)
    {
        public static readonly Multiplier Red = new(MultiplierType.Word, 3);
        public static readonly Multiplier Blue = new(MultiplierType.Letter, 3);
        public static readonly Multiplier LightBlue = new(MultiplierType.Letter, 2);
        public static readonly Multiplier Pink = new(MultiplierType.Word, 2);
    }
}
