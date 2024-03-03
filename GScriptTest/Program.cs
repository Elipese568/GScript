using GScript.Analyzer;

namespace GScriptTest;

internal partial class Program
{
    static int Main(string[] args)
    {
        //GSFunction.Test();
        Script gss = new Script();
        gss.Open("test.gs");
        LoadProc(in gss);
        bool result = gss.Execute();
        if(result == true)
        {
            return 0;
        }
        else
        {
            return ExceptionOperator.GetLastError();
        }
    }
}