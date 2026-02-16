using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace UglyToad.PdfPig.Benchmarks;

internal class NuGetPackageConfig : ManualConfig
{
    public NuGetPackageConfig()
    {
        var baseJob = Job.Default;

        var localJob = baseJob
            .WithMsBuildArguments("/p:PdfPigVersion=Local")
            .WithId("Local");

        var latestJob = baseJob
            .WithMsBuildArguments("/p:PdfPigVersion=Latest")
            .WithId("Latest")
            .AsBaseline();

        AddJob(localJob.WithRuntime(CoreRuntime.Core80));
        AddJob(latestJob.WithRuntime(CoreRuntime.Core80));
    }
}