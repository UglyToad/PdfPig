using BenchmarkDotNet.Running;

namespace UglyToad.PdfPig.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SystemFontFinderBenchmarks>();
        Console.ReadKey();
    }
}