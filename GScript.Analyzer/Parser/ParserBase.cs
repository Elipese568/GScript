namespace GScript.Analyzer.Parser;

public abstract class ParserBase
{
    public class EmptyParser : ParserBase
    {
        public override List<Token> ComplateParserTokens => null;

        public override bool Parse(string line) => true;

        public EmptyParser() : base("$$SPECIAL_EMPTY_PARSER$$") { }
    }

    public abstract List<Token> ComplateParserTokens { get; }

    public abstract bool Parse(string line);

    protected string m_command;

    public string CommandName => m_command;

    public static readonly ParserBase Empty = new EmptyParser();

    public ParserBase(string command) => m_command = command;
}
