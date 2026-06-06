using BenchmarkDotNet.Running;

namespace UglyToad.PdfPig.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        // Pass class names on the command line to pick benchmarks, e.g.:
        //     dotnet run -c Release -- --filter *ShadingAndColorBenchmarks*
        // When no args are supplied default to the shading/colour suite that the
        // feature/optimisations-shading work targets.
        if (args.Length == 0)
        {
            BenchmarkRunner.Run<ShadingAndColorBenchmarks>();
        }
        else
        {
            BenchmarkSwitcher.FromTypes(new[]
            {
                typeof(ShadingAndColorBenchmarks),
                typeof(SystemFontFinderBenchmarks),
                typeof(BruteForceBenchmarks),
                typeof(LayoutAnalysisBenchmarks),
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