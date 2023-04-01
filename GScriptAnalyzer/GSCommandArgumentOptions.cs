namespace GScriptAnalyzer;

using GScriptAnalyzer.Exception;
using Arguments = System.Collections.Generic.List<GSObject>;

public class GSCommandArgumentOptions
{
    public Range CountRange { get; set; }

    public bool CountIsRange { get; set; }
    public bool VaildArgumentCount { get; set; }
    public bool VaildArgumentType { get; set; }
    public bool VaildArgumentParenthesis { get; set; }

    public List<Util.ParenthesisType> ArgumentParenthesisTypePairs { get; set; }
    public List<Type> ArgumentTypePairs { get; set; }

    public static bool VerifyArgumentFromOptions(GSCommand cmd, GSCommandArgumentOptions options)
    {
        if(options.VaildArgumentCount && !VerifyArgumentRange(cmd.Args,options.CountRange))
        {
            GSPublic.SetLastError(GSPublic.GSErrorCode.GSE_ARGUMENTOUTOFRANGE);
            GSPublic.SetException(
                new ArgumentOutOfRangeException(
                    $"Argument count out of range " +
                    $"(this range is {options.CountRange.Start.Value}" +
                               $" to {options.CountRange.End.Value})"
                )
            );
            return false;
        }

        if(options.VaildArgumentType && !VerifyArgumentType(cmd.Args, options.ArgumentTypePairs))
        {
            GSPublic.SetLastError(GSPublic.GSErrorCode.GSE_WRONGARG);
            GSPublic.SetException(new ArgumentException("Argument type is wrong."));
            return false;
        }

        if(options.VaildArgumentParenthesis && !VerifyArgumentParenthesis(cmd, options))
        {
            GSPublic.SetLastError(GSPublic.GSErrorCode.GSE_WRONGARG);
            GSPublic.SetException(new ArgFormatException("Argument parenthesis is wrong."));
            return false;
        }
        return true;
    }

    public static bool VerifyArgumentRange(Arguments args, Range countrange) 
        => countrange.Start.Value <= args.Count && args.Count <= countrange.End.Value;

    public static bool VerifyArgumentType(Arguments args, List<Type> types)
    {
        for(int i = 0; i < args.Count; i++)
        {
            if (args[i].ValueType != types[i])
                return false;
        }
        return true;
    }

    public static bool VerifyArgumentParenthesis(GSCommand command, GSCommandArgumentOptions options)
    {
        if (!options.VaildArgumentParenthesis)
            return true;

        for(int i = 0; i < command.Args.Count; i ++)
        {
            if (options.ArgumentParenthesisTypePairs[i] != command.TypeArgPairs[command.Args[i]])
                return false;
        }

        return true;
    }


}
