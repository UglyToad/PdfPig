using BenchmarkDotNet.Running;

namespace UglyToad.PdfPig.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        // Select a benchmark class with BenchmarkDotNet's command-line filter, for example:
        //     dotnet run -c Release -- --filter *CcittFaxFilterBenchmarks*
        // With no arguments, run Type4FunctionBenchmarks.
        if (args.Length == 0)
        {
            BenchmarkRunner.Run<Type4FunctionBenchmarks>();
        }
        else
        {
            BenchmarkSwitcher.FromTypes(new[]
            {
                typeof(ShadingAndColorBenchmarks),
                typeof(SystemFontFinderBenchmarks),
                typeof(BruteForceBenchmarks),
                typeof(LayoutAnalysisBenchmarks),
                typeof(Type4FunctionBenchmarks),
                typeof(LzwFilterBenchmarks),
                typeof(PngPredictorBenchmarks),
                typeof(FlateFilterBenchmarks),
                typeof(CcittFaxFilterBenchmarks),
                typeof(IccColorManagementBenchmarks),
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