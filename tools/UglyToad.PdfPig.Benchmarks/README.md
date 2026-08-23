# UglyToad.PdfPig.Benchmarks

BenchmarkDotNet suites for PdfPig, plus the documents they read.

## Jobs

`NuGetPackageConfig` runs each benchmark twice on .NET 8:

| Job | PdfPig |
|---|---|
| `Latest` (baseline) | the released package pinned in `UglyToad.PdfPig.Benchmarks.csproj` |
| `Local` | the projects in `src/`, i.e. the working copy |

So `Ratio` and `Alloc Ratio` read as "the working copy relative to the last release" - below 1.00 is faster or leaner.
Bump the pinned package version when a release makes the comparison stale.

The project is compiled once per job, and the `Latest` job compiles it against the released package, so a
benchmark that uses API the release does not have will not build there.

## Running

```bash
# everything
dotnet run --project UglyToad.PdfPig.Benchmarks.csproj -c Release -- --filter '*'

# one suite
dotnet run --project UglyToad.PdfPig.Benchmarks.csproj -c Release -- --filter '*ColorOperatorBenchmarks*'

# quick pass while iterating (5-8s per case instead of 15-25s)
dotnet run --project UglyToad.PdfPig.Benchmarks.csproj -c Release -- --filter '*' --job Short
```

Reports land in `BenchmarkDotNet.Artifacts/results/`; the `-report-github.md` files are the readable ones.
Pass the project explicitly - this directory also holds a `.slnx`, which `dotnet run` will otherwise pick up.

## Suites

| Suite | What it covers |
|---|---|
| `BruteForceBenchmarks` | whole-document text extraction, from a page of text up to `algo.pdf` |
| `LayoutAnalysisBenchmarks` | word extraction and page segmentation over a fixed set of letters |
| `SystemFontFinderBenchmarks` | locating and reading installed fonts |
| `Type4FunctionBenchmarks` | the PostScript calculator function interpreter |
| `ShadingAndColorBenchmarks` | shadings, and images in Indexed / CalRGB / Separation spaces |
| `ColorOperatorBenchmarks` | the colour operators and the graphics state that carries their result |
| `IccProfileBenchmarks` | documents declaring ICC profiles and output intents, read with the default options |
| `IccProfileOptInBenchmarks` | what turning colour management *on* costs, against the same documents with it off |
