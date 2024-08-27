namespace GScript.WinForm
{
    internal static class Program
    {
        static Form1 MainWindow { get; set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            MainWindow = new Form1();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.Run(MainWindow);

            Analyzer.Script script = new Analyzer.Script();

            script.RegisterCommandHandler("CreateControl", (
            GSCommandHandler,
            new CommandArgumentOptions()
            {
                CountRange = new Range(0, 2),
                VaildArgumentCount = true,
                VaildArgumentType = false,
                VaildArgumentParenthesis = true
            }
        ));
            script.Open(args[0]);
        }
    }
}