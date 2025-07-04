namespace GScript.Analyzer.Parser;

public readonly struct ParserTokenMetaData
{
    public readonly ParserTokenType Type { get; }

    public readonly string Separator { get; }

    public readonly object? CustomData { get; }

    private ParserTokenMetaData(ParserTokenType type, string separator, object CustomData)
    {
        Type = type;
        Separator = separator;
        this.CustomData = CustomData;
    }

    public static ParserTokenMetaData MakeTokenData(ParserTokenType type, string separator, object CustomData = null)
    {
        return new ParserTokenMetaData(type, separator, CustomData);
    }
}
