using GScript.Analyzer.InternalType;

namespace GScript.Analyzer.Parser;

public readonly struct Token
{
    public readonly ParserTokenMetaData TokenMetaData { get; }
    public readonly ScriptObject TokenData { get; }
    public readonly string RawToken { get; }

    private Token(string rawToken, ParserTokenMetaData metaData, ScriptObject tokenData)
    {
        TokenMetaData = metaData;
        TokenData = tokenData;
        RawToken = rawToken;
    }

    public static Token MakeToken(string rawToken, ParserTokenMetaData metaData, ScriptObject tokenData) => new Token(rawToken, metaData, tokenData);
}
