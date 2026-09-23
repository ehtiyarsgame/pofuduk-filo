public static class Program
{
    // Runs the EditMode tests that need no native Unity engine (pure C# / managed math).
    public static int Main(string[] args) =>
        new NUnitLite.AutoRun(typeof(Program).Assembly).Execute(args);
}
