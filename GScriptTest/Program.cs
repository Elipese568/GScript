using GScriptAnalyzer;

namespace GScriptTest;

internal partial class Program
{
    static int Main(string[] args)
    {
        //GSFunction.Test();
        GSScript gss = new GSScript();
        gss.Open("test.gs");
        LoadProc(in gss);
        bool result = gss.Execute();
        if(result == true)
        {
            return 0;
        }
        else
        {
            return GSPublic.GetLastError();
        }
    }
}