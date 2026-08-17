using BenchmarkDotNet.Running;

namespace UglyToad.PdfPig.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            BenchmarkRunner.Run<IccProfileBenchmarks>();
        }
        else
        {
            BenchmarkSwitcher.FromTypes(new[]
            {
                typeof(IccProfileBenchmarks),
                typeof(ColorOperatorBenchmarks),
                typeof(ShadingAndColorBenchmarks),
                typeof(SystemFontFinderBenchmarks),
                typeof(BruteForceBenchmarks),
                typeof(LayoutAnalysisBenchmarks),
                typeof(Type4FunctionBenchmarks),
            }).Run(args);
        }

        // Only pause for a key when running interactively; CI / --list runs redirect stdin and
        // calling ReadKey() in that case throws.
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey();
        }
    }
}
