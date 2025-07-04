using GScript.Analyzer.InternalType;

namespace GScript.Analyzer.Executing;

using EUtility.ValueEx;
using GScript.Analyzer;
using GScript.Analyzer.Exception;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Arguments = List<ScriptObject>;

public class CommandArgumentOptions
{
    public Range CountRange { get; set; }

    public bool CountIsRange { get; set; }
    public bool VaildArgumentCount { get; set; }
    public bool VaildArgumentType { get; set; }
    public bool VaildArgumentParenthesis { get; set; }

    public List<Util.ParenthesisType> ArgumentParenthesisTypePairs { get; set; } = new();
    public List<Union<string, Type>> ArgumentTypePairs { get; set; } = new();

    public static bool VerifyArgumentFromOptions(Command cmd, CommandArgumentOptions options)
    {
        if (options.VaildArgumentCount && !VerifyArgumentRange(cmd.Args, options.CountRange))
        {
            ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_ARGUMENTOUTOFRANGE);
            ExceptionOperator.SetException(
                new ArgumentOutOfRangeException(
                    $"Argument count out of range " +
                    $"(this range is {options.CountRange.Start.Value}" +
                               $" to {options.CountRange.End.Value})"
                )
            );
            return false;
        }

        if (options.VaildArgumentType && !VerifyArgumentType(cmd.Args, options.ArgumentTypePairs))
        {
            ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
            ExceptionOperator.SetException(new ArgumentException("Argument type is wrong."));
            return false;
        }

        if (options.VaildArgumentParenthesis && !VerifyArgumentParenthesis(cmd, options))
        {
            ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
            ExceptionOperator.SetException(new ArgFormatException("Argument parenthesis is wrong."));
            return false;
        }
        return true;
    }

    public static bool VerifyArgumentRange(Arguments args, Range countrange)
        => countrange.Start.Value <= args.Count && args.Count <= countrange.End.Value;

    public static bool VerifyArgumentType(Arguments args, List<Union<string, Type>> types)
    {
        for (int i = 0; i < args.Count; i++)
        {
            var vtc = ~args[i].ValueType.Clone().As<Union<string, Type>>();
            var tc = ~types[i].Clone().As<Union<string, Type>>();
            if (!vtc.Equals(tc) && !tc.Equals(typeof(object)))
                return false;
            continue;
        }
        return true;
    }

    public static bool VerifyArgumentParenthesis(Command command, CommandArgumentOptions options)
    {
        if (!options.VaildArgumentParenthesis)
            return true;

        for (int i = 0; i < command.Args.Count; i++)
        {
            if (command.TypeArgPairs[command.Args[i]] != Util.ParenthesisType.Unknown && options.ArgumentParenthesisTypePairs[i] != Util.ParenthesisType.Unknown && options.ArgumentParenthesisTypePairs[i] != command.TypeArgPairs[command.Args[i]])
                return false;
        }
        return true;
    }


}
