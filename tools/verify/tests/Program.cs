using System;
using System.IO;
using PofudukFilo.Core;

public static class Program
{
    // Runs the EditMode tests that need no native Unity engine (pure C# / managed math).
    // "--loc-check <file>": translate each line and print the ones that stay Turkish.
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--loc-check")
        {
            int missing = 0;
            foreach (string raw in File.ReadAllLines(args[1]))
            {
                string line = raw.Replace("\\n", "\n");
                string en = Loc.ToEnglish(line);
                if (en.IndexOfAny("çğıöşüÇĞİÖŞÜâ".ToCharArray()) >= 0 && !IsName(en))
                {
                    Console.WriteLine("untranslated: " + raw + "  ->  " + en);
                    missing++;
                }
            }
            Console.WriteLine(missing == 0 ? "loc OK" : $"loc: {missing} untranslated");
            return missing == 0 ? 0 : 1;
        }
        return new NUnitLite.AutoRun(typeof(Program).Assembly).Execute(args);
    }

    // Brand/character names keep their Turkish letters in English on purpose.
    private static bool IsName(string s) => s is "Türkçe";
}
