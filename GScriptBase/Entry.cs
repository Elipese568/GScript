using EUtility.ValueEx;
using GScript.Analyzer;
using GScript.Analyzer.Exception;
using GScript.Analyzer.Util;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using System.Transactions;

namespace GScript.Standard;
using Arguments = ICollection<GScript.Analyzer.ScriptObject>;

internal class Entry
{
    public const int GSBE_INLINEFUNCINVAILD = 0x1000;
    public const int GSBE_FUNCISNTEXISTS = 0x1001;
    public const int GSBE_INVAILDCRITIALVARIABLEOPERATOR = 0x2001;
    public const int GSBE_VARIABLEISEXISTS = 0x2002;
    public const int GSBE_VARIABLEISNTEXISTS = 0x2003;
    public const int GSBE_STACKPOINTEROUTOFRANGE = 0x8000;


    public const string M_CallRet = "$RET$";
    public const string M_Argument1 = "$ARG$";
    public const string M_Argument2 = "$ARG1$";
    public const string M_Argument3 = "$ARG2$";
    public const string M_Argument4 = "$ARG3$";
    public const string M_Argument5 = "$ARG4$";
    public const string M_Argument6 = "$ARG5$";
    public const string M_Argument7 = "$ARG6$";
    public const string M_Argument8 = "$ARG7$";
    public const string M_SelfObject = "$SELF$";
    public const string M_ValueStackTop = "$VST$";
    public const string M_ValueStackPointer = "$VSP$";
    public const string M_CommandResult = "$CRET$";


    public Entry(string path)
    {
        _script = new();

        Initialize(in _script);

        _script.Open(path);

        _script.Vars.Add(M_CallRet, new("$RET$"));
        _script.Vars.Add(M_Argument1, new("$ARG$"));
        _script.Vars.Add(M_Argument2, new("$ARG1$"));
        _script.Vars.Add(M_Argument3, new("$ARG2$"));
        _script.Vars.Add(M_Argument4, new("$ARG3$"));
        _script.Vars.Add(M_Argument5, new("$ARG4$"));
        _script.Vars.Add(M_Argument6, new("$ARG5$"));
        _script.Vars.Add(M_Argument7, new("$ARG6$"));
        _script.Vars.Add(M_Argument8, new("$ARG7$"));
        _script.Vars.Add(M_SelfObject, new (M_SelfObject));
        _script.Vars.Add(M_ValueStackTop, new(M_ValueStackTop));
        _script.Vars.Add(M_ValueStackPointer, new(M_ValueStackPointer));
        _script.Vars.Add(M_CommandResult, new(M_CommandResult));

        for(int i = 0; i < 1024 * 8; i ++)
        {
            _valueStack.Add(new());
        }

        var result = _script.Execute();

        if(!result)
        {
            Console.WriteLine(ExceptionOperator.GetLastError());
            Console.WriteLine(ExceptionOperator.GetException());
            Console.WriteLine(ExceptionOperator.GetErrorData().Message);
            Console.WriteLine("unhandle exception");
        }
        
    }

    private Script _script;

    private Dictionary<string, Range> _funcTable = new();

    private bool _inFuncBlock = false;

    private bool _inClassBlock = false;

    private (string, Range) _currentFunc = new();

    private ClassTemplate _currentClass = null;

    private Stack<int> _callStack = new(1024*8);

    private List<object> _valueStack = new(1024 * 8);

    private Dictionary<Tag,int> _tags = new();

    private Dictionary<string, ClassTemplate> _classTemplateTable = new();

    private bool _noRun = false;

    private string[] _alwaysRun = new string[]
    {
        "defFunc",
        "defFuncEnd",
        "defClass",
        "defClassEnd",
        "prop",
        "defCFunc",
        "defCFuncEnd",
        "ctor",
        "ctorEnd",
        "tag"
    };

    private string[] _attributes = new string[]
    {
        "NoRun"
    };

    private void Initialize(in Script script)
    {
        bool OutCommand(Command cmd, ref int line)
        {
            var arg = cmd.Args;
            if (arg.Count == 1)
            {
                switch (cmd.TypeArgPairs[arg[0]])
                {
                    case GScript.Analyzer.Util.ParenthesisType.Small:
                    case GScript.Analyzer.Util.ParenthesisType.Middle:
                        string s = arg[0].Value.ToString();
                        Console.Write(StringHelper.ToCSString(s));
                        return true;
                    default:
                        ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                        ExceptionOperator.SetException(new ArgFormatException("Argument parenthesis should small or middle"));
                        return false;
                }

            }
            else if (arg.Count == 2)
            {
                if (cmd.TypeArgPairs[arg[0]] == GScript.Analyzer.Util.ParenthesisType.Small)
                {
                    if (cmd.TypeArgPairs[arg[0]] == GScript.Analyzer.Util.ParenthesisType.Small)
                        Console.WriteLine(arg[0].Value);
                    return true;
                }
                Console.WriteLine(arg[0].Value);
            }
            return true;
        }

        bool InputCommand(Command cmd, ref int line)
        {
            var arg = cmd.Args;
            if (cmd.TypeArgPairs[arg[0]] == GScript.Analyzer.Util.ParenthesisType.Small)
            {
                //GSScript.CurrentScript.Vars[(arg[0] as GSVar).Name].Value = Console.ReadLine();
                arg[0].Value = Console.ReadLine();
                return true;
            }
            ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
            return false;
        }

        bool DefFuncCommand(Command cmd, ref int line)
        {
            if (_inFuncBlock)
            {
                ExceptionOperator.SetLastError(GSBE_INLINEFUNCINVAILD);
                return false;
            }

            if (_callStack.Count > 0)
                return true;

            _inFuncBlock = true;

            var name = (cmd.Args[0].Value as Flag);

            _currentFunc.Item1 = name.FlagName + "::" + name.FlagValue;
            _currentFunc.Item2 = new(line, line + 1);
            return true;
        }

        bool DefFuncEndCommand(Command cmd, ref int line)
        {
            if(_inFuncBlock)
            {
                _inFuncBlock = false;

                _currentFunc.Item2 = new(_currentFunc.Item2.Start, line);

                _funcTable.Add(_currentFunc.Item1, _currentFunc.Item2);
            }

            if(_callStack.Count > 0)
            {
                if(cmd.Args.Count > 0)
                    Script.CurrentScript.SetVar(M_CallRet, cmd.Args[0].Value);
                
                line = _callStack.Pop();
            }

            return true;
        }

        bool CallCommand(Command cmd, ref int line)
        {
            if (!_funcTable.ContainsKey((cmd.Args[0].Value as Flag).FlagValue))
            {
                ExceptionOperator.SetLastError(GSBE_FUNCISNTEXISTS);
                return false;
            }

            if(cmd.Args.Count > 1)
            {
                for(int i = 1; i < cmd.Args.Count; i++)
                {
                    Script.CurrentScript.SetVar($"$ARG{i}$", cmd.Args[i].Value);
                }
            }

            _callStack.Push(line);
            var name = (cmd.Args[0].Value as Flag);
            line = _funcTable[name.FlagName + "::" + name.FlagValue].Start.Value;
            return true;
        }

        bool SysTypeStaticCallCommand(Command cmd, ref int line)
        {
            ObjectType? Type = (cmd.Args[0] as ObjectType);
            Type t = Type.Value as Type;
            var method = t.GetMethod(cmd.Args[1].Value as string, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, cmd.Args.Count > 2 ? cmd.Args.ToArray()[2..].Select(x => DeBox(x).GetType()).ToArray() : Array.Empty<Type>());

            List<object> param = new();

            int i = 0;
            foreach (var pi in method.GetParameters())
            {
                param.Add(Convert.ChangeType(cmd.Args[2 + i].Value, pi.ParameterType));
                i++;
            }

            if (method.ReturnType == typeof(void))
            {
                method.Invoke(null, param.ToArray());
            }
            else
            {
                Script.CurrentScript.SetVar(M_CommandResult, method.Invoke(cmd.Args[0].Value, param.ToArray()));
            }

            return true;
        }

        bool AssignCommand(Command cmd, ref int line)
        {
            if (cmd.Args[0] as Variable != null && (cmd.Args[0] as Variable).Name == M_ValueStackTop)
            {
                ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR); 
                return false;
            }
            cmd.Args[0].Value = cmd.Args[1].Value;
            return true;
        }

        bool DefDyVarCommand(Command cmd, ref int line)
        {
            Flag? name = (cmd.Args[0] as Flag);
            if(Script.CurrentScript.Vars.ContainsKey(name.FlagName + "::" + name.FlagValue))
            {
                ExceptionOperator.SetLastError(GSBE_VARIABLEISEXISTS);
                return false;
            }
            Script.CurrentScript.AddVar(name.FlagName + "::" + name.FlagValue);
            if(cmd.Args.Count == 2)
            {
                Script.CurrentScript.SetVar(name.FlagValue, cmd.Args[1].Value);
            }
            return true;
        }

        bool RemovVarCommand(Command cmd, ref int line)
        {
            var i = (cmd.Args[0] as Flag);
            if (!Script.CurrentScript.Vars.ContainsKey(i.FlagName + "::" + i.FlagValue))
            {
                ExceptionOperator.SetLastError(GSBE_VARIABLEISNTEXISTS);
                return false;
            }
            Script.CurrentScript.RemoveVar(i.FlagName + "::" + i.FlagValue);
            return true;
        }

        #region Operator
        bool AddCommand(Command cmd, ref int line)
        {
            int lineclone = line;
            Dictionary<(Type, Type), Func<object, object, bool>> AddProc = new()
            {
                [(TypeField.String, TypeField.Char)] = AddString,
                [(TypeField.String, TypeField.String)] = AddString,
                [(TypeField.Char, TypeField.String)] = AddString,
                [(TypeField.Char, TypeField.Char)] = AddString,
                [(TypeField.Long, TypeField.Long)] = AddNumber_LL,
                [(TypeField.Numeric.Int128, TypeField.Numeric.Int128)] = AddNumber_L128,
                [(TypeField.Long, TypeField.Numeric.Int128)] = AddNumber_L128,
                [(TypeField.Numeric.Int128, TypeField.Long)] = AddNumber_L128,
                [(TypeField.Numeric.BigInteger, TypeField.Numeric.BigInteger)] = AddNumber_NL,
                [(TypeField.Long, TypeField.Numeric.BigInteger)] = AddNumber_NL,
                [(TypeField.Numeric.BigInteger, TypeField.Long)] = AddNumber_NL,
                [(TypeField.Numeric.Int128, TypeField.Numeric.BigInteger)] = AddNumber_NL,
                [(TypeField.Numeric.BigInteger, TypeField.Numeric.Int128)] = AddNumber_NL
            };

            bool AddString(object a, object b)
            {
                string aStr = a.ToString();
                string bStr = b.ToString();
                Script.CurrentScript.SetVar(M_CommandResult, aStr + bStr);
                return true;
            }

            bool AddNumber_LL(object a, object b)
            {
                long aNumber = (long)a;
                long bNumber = (long)b;
                try
                {
                    Script.CurrentScript.SetVar(M_CommandResult, aNumber + bNumber);
                    return true;
                }
                catch(OverflowException e)
                {
                    ErrorData ed = new(lineclone, cmd.ToCommandString(), e, "Add operation overflow (number + number).");
                    ExceptionOperator.SetLastErrorEx(ed);
                    return false;
                }
            }

            bool AddNumber_L128(object a, object b)
            {
                Int128 aLongNumber = (Int128)a;
                Int128 bLongNumber = (Int128)b;
                try
                {
                    Script.CurrentScript.SetVar(M_CommandResult, aLongNumber + bLongNumber);
                    return true;
                }
                catch (OverflowException e)
                {
                    ErrorData ed = new(lineclone, cmd.ToCommandString(), e, "Add operation overflow (number + number).");
                    ExceptionOperator.SetLastErrorEx(ed);
                    return false;
                }
            }

            bool AddNumber_NL(object a, object b)
            {
                BigInteger aLargerNumber = (BigInteger)a;
                BigInteger bLargerNumber = (BigInteger)b;
                try
                {
                    Script.CurrentScript.SetVar(M_CommandResult, aLargerNumber + bLargerNumber);
                    return true;
                }
                catch (OverflowException e)
                {
                    ErrorData ed = new(lineclone, cmd.ToCommandString(), e, "Add operation overflow (number + number).");
                    ExceptionOperator.SetLastErrorEx(ed);
                    return false;
                }
            }

            var a = cmd.Args[0];
            var b = cmd.Args[1];

            object avt = (~a.ValueType.Clone().As<Union<string, System.Type>>());
            object bvt = (~b.ValueType.Clone().As<Union<string, System.Type>>());

            Type aType = avt as Type ?? typeof(void);
            Type bType = bvt as Type ?? typeof(void);

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (long)v.Value + (long)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (AddProc.TryGetValue((aType, bType), out Func<object, object, bool> func))
            {
                return func(a.Value, b.Value);
            }
            else
            {
                ErrorData ed = new(line, cmd.ToCommandString(), ExceptionOperator.GErrorCode.GSE_WRONGARG, "invaild arg.");
                ExceptionOperator.SetLastErrorEx(ed);
            }

            //Script.CurrentScript.SetVar(M_CommandResult, (long)a.Value + (long)b.Value);
            return true;
        }

        bool SubCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (long)v.Value - (long)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Long || b.ValueType != TypeField.Long)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (long)a.Value - (long)b.Value);
            return true;
        }

        bool MulCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (long)v.Value * (long)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Long || b.ValueType != TypeField.Long)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (long)a.Value * (long)b.Value);
            return true;
        }

        bool DivCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (long)v.Value / (long)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Long || b.ValueType != TypeField.Long)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (long)a.Value / (long)b.Value);
            return true;
        }

        bool AddFCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Double || b.ValueType != TypeField.Double)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (double)a.Value + (double)b.Value);
            return true;
        }

        bool SubFCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Double || b.ValueType != TypeField.Double)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (double)a.Value - (double)b.Value);
            return true;
        }

        bool MulFCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false; ;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Double || b.ValueType != TypeField.Double)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (double)a.Value * (double)b.Value);
            return true;
        }

        bool DivFCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Double || b.ValueType != TypeField.Double)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (double)a.Value / (double)b.Value);
            return true;
        }

        bool AddLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (Int128)v.Value + (Int128)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.Int128 || b.ValueType != TypeField.Numeric.Int128)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (Int128)a.Value + (Int128)b.Value);
            return true;
        }

        bool SubLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (Int128)v.Value - (Int128)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.Int128 || b.ValueType != TypeField.Numeric.Int128)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (Int128)a.Value - (Int128)b.Value);
            return true;
        }

        bool MulLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (Int128)v.Value * (Int128)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.Int128 || b.ValueType != TypeField.Numeric.Int128)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (Int128)a.Value * (Int128)b.Value);
            return true;
        }

        bool DivLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (Int128)v.Value / (Int128)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.Int128 || b.ValueType != TypeField.Numeric.Int128)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (Int128)a.Value / (Int128)b.Value);
            return true;
        }

        bool AddBLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (BigInteger)v.Value + (BigInteger)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.BigInteger || b.ValueType != TypeField.Numeric.BigInteger)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (BigInteger)a.Value + (BigInteger)b.Value);
            return true;
        }

        bool SubBLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (BigInteger)v.Value - (BigInteger)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.BigInteger || b.ValueType != TypeField.Numeric.BigInteger)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (BigInteger)a.Value - (BigInteger)b.Value);
            return true;
        }

        bool MulBLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (BigInteger)v.Value * (BigInteger)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.BigInteger || b.ValueType != TypeField.Numeric.BigInteger)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (BigInteger)a.Value * (BigInteger)b.Value);
            return true;
        }

        bool DivBLCommand(Command cmd, ref int line)
        {
            var a = cmd.Args[0];
            var b = cmd.Args[1];

            if (a as Variable != null)
            {
                var v = (a as Variable);
                switch (v.Name)
                {
                    case M_ValueStackPointer:
                        _script.SetVar(M_ValueStackPointer, (BigInteger)v.Value / (BigInteger)b.Value);
                        return true;
                    case M_ValueStackTop:
                        ExceptionOperator.SetLastError(GSBE_INVAILDCRITIALVARIABLEOPERATOR);
                        return false;
                    default:
                        break;
                }
            }

            if (a.ValueType != TypeField.Numeric.BigInteger || b.ValueType != TypeField.Numeric.BigInteger)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                ExceptionOperator.SetException(new ArgumentException("Different type execute 'Add' operation is invaild."));
                return false;
            }

            Script.CurrentScript.SetVar(M_CommandResult, (BigInteger)a.Value / (BigInteger)b.Value);
            return true;
        }

        #endregion

        int GetValueStackVaildValueLength(List<object> valueStack)
        {
            for (int i = 0; i < valueStack.Count; i++)
            {
                if (valueStack[i].GetType() == TypeField.Object)
                    return i;
            }
            return 0;
        }

        bool PushCommand(Command cmd, ref int line)
        {
            int Pointer = (int)_script.Vars[M_ValueStackPointer].Value;
            int length = GetValueStackVaildValueLength(_valueStack);
            if (Pointer > length - 1 || 0 > Pointer)
            {
                ExceptionOperator.SetLastError(GSBE_STACKPOINTEROUTOFRANGE);
                return false;
            }
            else if(Pointer < length -1)
            {
                for (int i = length - 1; i > (length - 1 - Pointer); i--)
                {
                    (_valueStack[i], _valueStack[i + 1]) = (_valueStack[i + 1], _valueStack[i]);
                }
            }

            _valueStack[Pointer] = cmd.Args[0].Value;
            return true;
        }

        bool PullCommand(Command cmd, ref int line)
        {
            int Pointer = (int)_script.Vars[M_ValueStackPointer].Value;
            int length = GetValueStackVaildValueLength(_valueStack);
            object pv = new();
            if (Pointer < length - 1 || Pointer < 0)
            {
                ExceptionOperator.SetLastError(GSBE_STACKPOINTEROUTOFRANGE);
                return false;
            }
            pv = _valueStack[Pointer];
            if (length - 1 > Pointer)
            {
                for (int i = (length - 1 - Pointer); i < length; i++)
                {
                    if (i == 0)
                        continue;

                    // swap value of them
                    // inventor of this sentence is so sky god!!
                    (_valueStack[i], _valueStack[i - 1]) = (_valueStack[i - 1], _valueStack[i]);
                }
            }
            else
            {
                _valueStack[Pointer] = new();
            }
            
            _script.SetVar(M_CommandResult, pv);
            return true;
        }

        bool PeekCommand(Command cmd, ref int line)
        {
            int Pointer = (int)_script.Vars[M_ValueStackPointer].Value;
            int length = GetValueStackVaildValueLength(_valueStack);
            if (Pointer < length - 1 || Pointer < 0)
            {
                ExceptionOperator.SetLastError(GSBE_STACKPOINTEROUTOFRANGE);
                return false;
            }
            
            _script.SetVar(M_CommandResult, _script.Vars[M_ValueStackPointer].Value);
            return true;
        }

        bool TagCommand(Command cmd, ref int line)
        {
            _tags.Add(cmd.Args[0] as Tag, line);
            return true;
        }

        bool JmpInternal(object arg, string commandstring, ref int line)
        {
            if (arg is Tag tag)
            {
                line = _tags.First(x=>x.Key.TagName == tag.TagName).Value;
            }
            else if (arg is ScriptObject elsetag && elsetag.ValueType == TypeField.Int)
            {
                line = (int)elsetag.Value;
            }
            else
            {
                ErrorData ed = new(line, commandstring, new ArgumentException("Arg $0 type is wrong."), "Arg $0 type is wrong.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }
            return true;
        }

        bool JmpCommand(Command cmd, ref int line)
        {
            return JmpInternal(cmd.Args[0], cmd.ToCommandString(), ref line);
        }

        bool JmpcCommand(Command cmd, ref int line)
        {
            switch(cmd.Args.Count)
            {
                case 2:
                    if (cmd.Args[0].Value is bool c && c)
                    {
                        return JmpInternal(cmd.Args[1], cmd.ToCommandString(), ref line);
                    }
                    ErrorData ed = new(line, cmd.ToCommandString(), new ArgumentException("Arg type is wrong"), "Arg type is wrong");
                    ExceptionOperator.SetLastErrorEx(ed);
                    break;
                case 3:
                    if (cmd.Args[2].Value is bool iselse)
                    {
                        if (cmd.Args[0].Value is bool c2 && c2 == !iselse)
                        {
                            return JmpInternal(cmd.Args[1], cmd.ToCommandString(), ref line);
                        }
                        return true;
                    }
                    ErrorData ed2 = new(line, cmd.ToCommandString(), new ArgumentException("Arg type is wrong"), "Arg type is wrong");
                    ExceptionOperator.SetLastErrorEx(ed2);
                    break;
            }
            return false;
        }

        bool NoRunTrueCommand(Command cmd, ref int line)
        {
            _noRun = true;
            return true;
        }

        bool NoRunFalseCommand(Command cmd, ref int line)
        {
            _noRun = false;
            return true;
        }

        bool CompareInternal(object left, object right, string symbol)
        {
            try
            {
                switch (symbol)
                {
                    case "==":
                        return left.Equals(right);
                    case "!=":
                        return !left.Equals(right);
                    case ">":
                        return (left as IComparable).CompareTo(right) > 0;
                    case ">=":
                        return (left as IComparable).CompareTo(right) is > 0 or 0;
                    case "<":
                        return (left as IComparable).CompareTo(right) < 0;
                    case "<=":
                        return (left as IComparable).CompareTo(right) is < 0 or 0;
                }
            }
            catch { }
            return false;
        }

        bool CompCommand(Command cmd, ref int line)
        {
            Script.CurrentScript.SetVar(M_CommandResult, CompareInternal(cmd.Args[1].Value, cmd.Args[2].Value, (cmd.Args[0] as Flag).FlagValue));
            return true;
        }

        bool ExitCommand(Command cmd, ref int line)
        {
            if(cmd.Args.Count == 1 && cmd.Args[0].Value is int exitcode)
            {
                Environment.Exit(exitcode);
            }
            else
            {
                Environment.Exit(0);
            }
            return true;
        }

        bool DefClassCommand(Command cmd, ref int line)
        {
            if (_inClassBlock || _inFuncBlock)
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Class in class."), "Class in class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            _inClassBlock = true;
            ClassTemplate ct = new(cmd.Args[0].Value as string, line);
            _currentClass = ct;
            return true;
        }

        bool PropCommand(Command cmd, ref int line)
        {
            if (!_inClassBlock || _inFuncBlock)
                return false;
            ClassProperty cp = new(cmd.Args[0].Value as string, (cmd.Args[1] as ObjectType).Value as Type);
            _currentClass.RegisterProp(cp.Name, cp);
            return true;
        }

        string GeneratorFunctionSignature(Arguments args, bool inDef = true)
        {
            List<string> ArgTypes = new List<string>();

            foreach (var arg in args)
            {
                if (arg is ObjectType objectType)
                {
                    ArgTypes.Add(inDef ? objectType.Value.ToString() : typeof(ObjectType).FullName);
                }
                else
                {
                    var T = ~arg.ValueType.Clone().As<Union<string, Type>>();
                    ArgTypes.Add(T.ToString());
                }
            }
            return string.Join('@', ArgTypes);
        }

        bool CtorCommand(Command cmd, ref int line)
        {
            if (!_inClassBlock || _inFuncBlock)
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Ctor. without class."), "Ctor. without class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            ClassFunction ctorfunc = new($".{_currentClass.Name}@{(cmd.Args.Count > 0 ? GeneratorFunctionSignature(cmd.Args.ToArray()) : "")}.ctor", Privation.Public, line);
            _inFuncBlock = true;

            _currentClass.RegisterFunc(ctorfunc.Name, ctorfunc);

            return true;
        }

        bool CtorEndCommand(Command cmd, ref int line)
        {
            if(_callStack.Count > 0)
            {
                if(cmd.Args.Count > 0)
                {
                    Script.CurrentScript.SetVar(M_CallRet, cmd.Args[0]);
                }
                line = _callStack.Pop();
                return true;
            }
            if(_inFuncBlock && _inClassBlock)
                return !(_inFuncBlock = false);
            else
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Ctor. end without class."), "Ctor. end without class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }
        }

        bool DefCFuncCommand(Command cmd, ref int line)
        {
            if (!_inClassBlock || _inFuncBlock)
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Member function without class."), "Member function without class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            if (_callStack.Count > 0)
                return true;

            ClassFunction func = new($".{_currentClass.Name}@{(cmd.Args.Count > 2 ? GeneratorFunctionSignature(cmd.Args.ToArray()[2..]) : "")}.{cmd.Args[0].Value as string}", (Privation)Enum.Parse(typeof(Privation), cmd.Args[1].Value as string), line);

            _currentClass.RegisterFunc(func.Name, func);

            _inFuncBlock = true;

            return true;
        }

        bool DefCFuncEndCommand(Command cmd, ref int line)
        {
            if (_callStack.Count > 0)
            {
                if (cmd.Args.Count > 0)
                {
                    Script.CurrentScript.SetVar(M_CallRet, cmd.Args[0]);
                }
                line = _callStack.Pop();
                return true;
            }

            if (_inFuncBlock && _inClassBlock)
                return !(_inFuncBlock = false);

            ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Member function without class."), "Member function without class.");
            ExceptionOperator.SetLastErrorEx(ed);
            return false;
        }

        // callM Variable(ValueType = Type) Type MemberName Args
        bool CallMemberCommand(Command cmd, ref int line)
        {
            int orgline = line;

            if ((cmd.Args[1] as ObjectType).Exists)
            {
                Type t = (cmd.Args[1] as ObjectType).Value as Type;
                var method = t.GetMethod(cmd.Args[2].Value as string, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, cmd.Args.Count > 3? cmd.Args.ToArray()[3..].Select(x => DeBox(x).GetType()).ToArray() : Array.Empty<Type>());

                List<object> param = new();

                int i = 0;
                foreach(var pi in method.GetParameters())
                {
                    param.Add(Convert.ChangeType(cmd.Args[3 + i].Value, pi.ParameterType));
                }

                if (method.ReturnType == typeof(void))
                {
                    method.Invoke(cmd.Args[0].Value, param.ToArray());
                }
                else
                {
                    Script.CurrentScript.SetVar(M_CommandResult, method.Invoke(cmd.Args[0].Value, param.ToArray()));
                }

                return true;
            }

            if (VariableIsDelcarClass(cmd))
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new ArgumentException("SELF object is not delcar. to this class."), "SELF object is not delcar. to this class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            var instance = (cmd.Args[0].Value as ClassInstance);
            ScriptObject[] argArray = cmd.Args.ToArray();
            bool complate = instance.Invoke($".{(cmd.Args[1] as ObjectType).Value as string}@{(cmd.Args.Count > 3 ? GeneratorFunctionSignature(cmd.Args.ToArray()[3..]) : "")}.{cmd.Args[2]}", ref line, cmd.Args.Count > 3 ? argArray[3..] : null);
            if(complate)
            {
                _callStack.Push(orgline);
            }
            return complate;
        }

        // getProp Variable Type MemberName
        bool GetPropCommand(Command cmd, ref int line)
        {
            if ((cmd.Args[1] as ObjectType).Exists)
            {
                Type t = (cmd.Args[1] as ObjectType).Value as Type;
                Script.CurrentScript.SetVar(M_CommandResult, t.GetProperty(cmd.Args[2].Value as string).GetValue(cmd.Args[0].Value));
                return true;
            }
            if (VariableIsDelcarClass(cmd))
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new ArgumentException("SELF object is not delcar. to this class."), "SELF object is not delcar. to this class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            var instance = (cmd.Args[0].Value as ClassInstance);
            try
            {
                Script.CurrentScript.SetVar(M_CommandResult, instance.GetProperty(cmd.Args[2].Value as string));
            }
            catch(KeyNotFoundException e)
            {
                ErrorData ed = new(line, cmd.ToCommandString(), e, "Member in " + instance.ClassName + "  no found.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }
            return true;
        }

        static bool VariableIsDelcarClass(Command cmd)
        {
            return ((cmd.Args[1] as ObjectType).Value as string) != (cmd.Args[0].Value as ClassInstance).ClassName;
        }

        object DeBox(ScriptObject scriptObject)
        {
            if(scriptObject is not Variable)
            {
                return scriptObject.Value;
            }
            else
            {
                return DeBox(scriptObject.Value as ScriptObject);
            }
        }

        bool SetPropCommand(Command cmd, ref int line)
        {
            if ((cmd.Args[1] as ObjectType).Exists)
            {
                Type t = (cmd.Args[1] as ObjectType).Value as Type;
                t.GetProperty(cmd.Args[2].Value as string).SetValue(cmd.Args[0].Value, DeBox(cmd.Args[3]));
                return true;
            }

            if (VariableIsDelcarClass(cmd))
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new ArgumentException("SELF object is not delcar. to this class."), "SELF object is not delcar. to this class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            var instance = (cmd.Args[0].Value as ClassInstance);
            return instance.SetProperty(cmd.Args[2].Value as string, DeBox(cmd.Args[3]));
        }

        bool DefClassEndCommand(Command cmd, ref int line)
        {
            if (!_inClassBlock || _inFuncBlock)
            {
                ErrorData ed = new(line, cmd.ToCommandString(), new InvaildScriptSegmentException("Ctor. without class."), "Ctor. without class.");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            _currentClass.ClassEnd(line);
            _classTemplateTable.Add(_currentClass.Name, _currentClass);
            _currentClass = null;

            return !(_inClassBlock = false);
        }

        // Variable Type Args
        bool InitCommand(Command cmd, ref int line)
        {
            int orgline = line;

            if(!_classTemplateTable.ContainsKey((cmd.Args[1] as ObjectType).Value as string))
            {
                ErrorData ed = new ErrorData(line, cmd.ToCommandString(), new ArgumentException("Class " + ((cmd.Args[1] as ObjectType).Value as string) + " not in this script. (maybe not include?)"), "Class " + ((cmd.Args[1] as ObjectType).Value as string) + " not in this script. (maybe not include?)");
                ExceptionOperator.SetLastErrorEx(ed);
                return false;
            }

            var instance = _classTemplateTable[(cmd.Args[1] as ObjectType).Value as string].CreateInstance(cmd.Args[0].Value as string);
            
            ScriptObject[] argArray = cmd.Args.ToArray();
            bool complate = instance.Invoke($".{(cmd.Args[1] as ObjectType).Value as string}@{(cmd.Args.Count > 2 ? GeneratorFunctionSignature(cmd.Args.ToArray()[2..]) : "")}.ctor", ref line, cmd.Args.Count > 2 ? argArray[2..] : null);
            if (!complate)
                return false;

            _callStack.Push(orgline);

            cmd.Args[0].Value = instance;
            cmd.Args[0].ValueType = typeof(ClassInstance);

            return complate;
        }

        void GlobalHandler(Command cmd, ref bool cancel, ref int line)
        {
            if (_alwaysRun.Contains(cmd.Name))
                return;

            foreach (var attributeitem in _attributes)
            {
                if (cmd.Name.Contains(attributeitem))
                    return;
            }
            
            cancel = _inFuncBlock || _noRun;
        }

        script.RegisterGlobalCommandHandler(GlobalHandler);

        #region Register Handles

        script.RegisterCommandHandler("out", (
            OutCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentParenthesis = false,
                VaildArgumentCount = true,
                VaildArgumentType = false,
                CountRange = new Range(1, 1)
            }
        ));

        // input command
        script.RegisterCommandHandler("input", (
            InputCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentType = false,
                VaildArgumentCount = true,
                VaildArgumentParenthesis = true,
                CountRange = new Range(1, 1),
                ArgumentParenthesisTypePairs = new List<GScript.Analyzer.Util.ParenthesisType>()
                { GScript.Analyzer.Util.ParenthesisType.Small}
            }
        ));

        script.RegisterCommandHandler("defFunc", (
            DefFuncCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentCount = true,
                VaildArgumentParenthesis = true,
                VaildArgumentType = false,
                CountRange = new Range(1,1),
                ArgumentParenthesisTypePairs = new List<GScript.Analyzer.Util.ParenthesisType>()
                { GScript.Analyzer.Util.ParenthesisType.Middle },
                ArgumentTypePairs = new()
                { typeof(Flag) }
            }
        ));

        script.RegisterCommandHandler("defFuncEnd", (
            DefFuncEndCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentCount = true,
                VaildArgumentParenthesis = false,
                VaildArgumentType = false,
                CountRange = new Range(0, 1),
            }
        ));

        script.RegisterCommandHandler("callFunc", (
            CallCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentCount = true,
                VaildArgumentParenthesis = true,
                VaildArgumentType = true,
                CountRange = new Range(1, 9),
                ArgumentParenthesisTypePairs = new List<GScript.Analyzer.Util.ParenthesisType>()
                { GScript.Analyzer.Util.ParenthesisType.Middle },
                ArgumentTypePairs = new ()
                { 
                    typeof(Flag), 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object, 
                    TypeField.Object  
                }
            }
        ));

        script.RegisterCommandHandler("assign", (
            AssignCommand,
            new CommandArgumentOptions()
            {
                VaildArgumentCount = true,
                VaildArgumentParenthesis = false,
                VaildArgumentType = false,
                CountRange = new Range(2, 2)
            }
        ));


        script.RegisterCommandHandler("remVar", (
            RemovVarCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentTypePairs = new ()
                { typeof(Flag) },
                ArgumentParenthesisTypePairs = new List<GScript.Analyzer.Util.ParenthesisType>
                { GScript.Analyzer.Util.ParenthesisType.Middle }
            }
        ));

        script.RegisterCommandHandler("defDyVar", (
            DefDyVarCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new List<GScript.Analyzer.Util.ParenthesisType>()
                { GScript.Analyzer.Util.ParenthesisType.Middle },
                ArgumentTypePairs = new ()
                { typeof(Flag) }
            }
        ));


        script.RegisterCommandHandler("pull", (
            PullCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("push", (
            PushCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("peek", (
            PeekCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));

        script.RegisterCommandHandler("add", (
            AddCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false,
            }
        ));

        script.RegisterCommandHandler("sub", (
            SubCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Long, TypeField.Long }
            }
        ));

        script.RegisterCommandHandler("mul", (
            MulCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Long, TypeField.Long }
            }
        ));

        script.RegisterCommandHandler("div", (
            DivCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Long, TypeField.Long }
            }
        ));

        script.RegisterCommandHandler("addF", (
            AddFCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Double, TypeField.Double }
            }
        ));

        script.RegisterCommandHandler("subF", (
            SubFCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Double, TypeField.Double }
            }
        ));

        script.RegisterCommandHandler("mulF", (
            MulFCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Double, TypeField.Double }
            }
        ));

        script.RegisterCommandHandler("divF", (
            DivFCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Double, TypeField.Double }
            }
        ));

        script.RegisterCommandHandler("addL", (
            AddLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.Int128, TypeField.Numeric.Int128 }
            }
        ));

        script.RegisterCommandHandler("subL", (
            SubLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.Int128, TypeField.Numeric.Int128 }
            }
        ));

        script.RegisterCommandHandler("mulL", (
            MulLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.Int128, TypeField.Numeric.Int128 }
            }
        ));

        script.RegisterCommandHandler("divL", (
            DivLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.Int128, TypeField.Numeric.Int128 }
            }
        ));

        script.RegisterCommandHandler("addBL", (
            AddBLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.BigInteger, TypeField.Numeric.BigInteger }
            }
        ));

        script.RegisterCommandHandler("subBL", (
            SubBLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.BigInteger, TypeField.Numeric.BigInteger }
            }
        ));

        script.RegisterCommandHandler("mulBL", (
            MulBLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.BigInteger, TypeField.Numeric.BigInteger }
            }
        ));

        script.RegisterCommandHandler("divBL", (
            DivBLCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                { TypeField.Numeric.BigInteger, TypeField.Numeric.BigInteger }
            }
        ));


        script.RegisterCommandHandler("tag", (
            TagCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                {
                    typeof(Tag)
                }
            }
        ));


        script.RegisterCommandHandler("jmp", (
            JmpCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("jmpc", (
            JmpcCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 3),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("comp", (
            CompCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(3, 3),
                VaildArgumentCount = true,
                VaildArgumentType = true,  
                VaildArgumentParenthesis = false,
                ArgumentTypePairs = new()
                {
                    typeof(Object), TypeField.Object, TypeField.Object
                }
            }
        ));


        script.RegisterCommandHandler("exit", (
            ExitCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 1),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("defClass", (
            DefClassCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(1, 1),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Middle
                },
                ArgumentTypePairs = new()
                {
                    "class"
                }
            }
        ));


        script.RegisterCommandHandler("ctor", (
            CtorCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 8),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big
                }
            }
        ));


        script.RegisterCommandHandler("ctorEnd", (
            CtorEndCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("defCFunc", (
            DefCFuncCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 10),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Middle,
                    ParenthesisType.Middle,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big,
                    ParenthesisType.Big
                },
                ArgumentTypePairs = new()
                {
                    "function",
                    "Privation",
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType),
                    typeof(ObjectType)
                }
            }
        ));


        script.RegisterCommandHandler("defCFuncEnd", (
            DefCFuncEndCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("prop", (
            PropCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 2),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Middle,
                    ParenthesisType.Big
                },
                ArgumentTypePairs = new()
                {
                    "property",
                    typeof(ObjectType)
                }
            }
        ));


        script.RegisterCommandHandler("callM", (
            CallMemberCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(3, 11),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Small,
                    ParenthesisType.Big,
                    ParenthesisType.Middle,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown
                },
                ArgumentTypePairs = new()
                {
                    typeof(object),
                    typeof(ObjectType),
                    "function",
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object
                }
            }
        ));


        script.RegisterCommandHandler("sysTypeStaticCall", (
            SysTypeStaticCallCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 10),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Big,
                    ParenthesisType.Middle,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown
                },
                ArgumentTypePairs = new()
                {
                    typeof(ObjectType),
                    "function",
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object,
                    TypeField.Object
                }
            }
        ));


        script.RegisterCommandHandler("getProp", (
            GetPropCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(3, 3),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Small,
                    ParenthesisType.Big,
                    ParenthesisType.Middle
                },
                ArgumentTypePairs = new()
                {
                    typeof(object),
                    typeof(ObjectType),
                    "property"
                }
            }
        ));


        script.RegisterCommandHandler("setProp", (
            SetPropCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(4, 4),
                VaildArgumentCount = true,
                VaildArgumentType = true,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Small,
                    ParenthesisType.Big,
                    ParenthesisType.Middle,
                    ParenthesisType.Unknown
                },
                ArgumentTypePairs = new()
                {
                    typeof(object),
                    typeof(ObjectType),
                    "property",
                    TypeField.Object
                }
            }
        ));


        script.RegisterCommandHandler("init", (
            InitCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(2, 10),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = true,
                ArgumentParenthesisTypePairs = new()
                {
                    ParenthesisType.Small,
                    ParenthesisType.Big,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown,
                    ParenthesisType.Unknown
                }
            }
        ));

        script.RegisterCommandHandler("defClassEnd", (
            DefClassEndCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));

        script.RegisterCommandHandler("$$NoRun?Enabled=True$$", (
            NoRunTrueCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));


        script.RegisterCommandHandler("$$NoRun?Enabled=False$$", (
            NoRunFalseCommand,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 0),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = false
            }
        ));

        #endregion
    }
}
