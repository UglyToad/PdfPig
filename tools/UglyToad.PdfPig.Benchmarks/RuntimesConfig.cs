using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// The local build on .NET 8 and on .NET 9, for code whose speed depends on the runtime rather
/// than on PdfPig: DeflateStream inflates through native zlib on .NET 8 and through zlib-ng
/// from .NET 9 on.
/// </summary>
internal class RuntimesConfig : ManualConfig
{
    public RuntimesConfig()
    {
        var local = Job.Default.WithMsBuildArguments("/p:PdfPigVersion=Local");

        AddJob(local.WithRuntime(CoreRuntime.Core80).WithId("net8.0").AsBaseline());
        AddJob(local.WithRuntime(CoreRuntime.Core90).WithId("net9.0"));
    }
}
