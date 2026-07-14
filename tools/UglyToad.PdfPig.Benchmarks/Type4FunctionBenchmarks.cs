using System.Reflection;
using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Functions;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Micro-benchmarks for the Type 4 (PostScript calculator) function interpreter.
/// PdfFunctionType4 is internal, so instances are constructed via reflection; the
/// public Eval members are invoked through cached delegates so only evaluation cost
/// is measured.
/// </summary>
[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class Type4FunctionBenchmarks
{
    // 1 input -> 1 output; arithmetic + conditional operators (tent function).
    private const string TentProgram = "{ dup 0.5 le { 2 mul } { 1 exch sub 2 mul } ifelse }";

    // 1 input -> 4 outputs; emulates a Separation tint transform to CMYK (arithmetic + stack operators).
    private const string TintProgram = "{ dup 0.85 mul exch dup 0.6 mul exch dup 0.2 mul exch 0.1 mul }";

    private readonly double[] input = { 0.42 };

    private Func<double[], double[]> evalTent = null!;
    private Func<double[], double[]> evalTint = null!;

    private PdfFunction tintFunction = null!;
    private readonly double[] outputBuffer = new double[4];

    [GlobalSetup]
    public void Setup()
    {
        object tent = CreateType4Function(TentProgram, new double[] { 0, 1 }, new double[] { 0, 1 });
        object tint = CreateType4Function(TintProgram, new double[] { 0, 1 }, new double[] { 0, 1, 0, 1, 0, 1, 0, 1 });

        evalTent = CreateArrayEvalDelegate(tent);
        evalTint = CreateArrayEvalDelegate(tint);

        tintFunction = (PdfFunction)tint;
    }

    [Benchmark]
    public double[] Tent_EvalArray() => evalTent(input);

    [Benchmark]
    public double[] Tint_EvalArray() => evalTint(input);

    [Benchmark]
    public int Tint_EvalSpan() => tintFunction.Eval(input, outputBuffer);

    private static object CreateType4Function(string program, double[] domain, double[] range)
    {
        Type type = typeof(PdfFunction).Assembly.GetType("UglyToad.PdfPig.Functions.PdfFunctionType4")
            ?? throw new InvalidOperationException("PdfFunctionType4 not found.");

        var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());
        var stream = new StreamToken(dictionary, System.Text.Encoding.ASCII.GetBytes(program));

        ConstructorInfo ctor = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)[0];
        return ctor.Invoke(new object[] { stream, ToArrayToken(domain), ToArrayToken(range) });
    }

    private static Func<double[], double[]> CreateArrayEvalDelegate(object function)
    {
        MethodInfo method = function.GetType().GetMethod("Eval", new[] { typeof(double[]) })
            ?? throw new InvalidOperationException("Eval(double[]) not found.");
        return (Func<double[], double[]>)method.CreateDelegate(typeof(Func<double[], double[]>), function);
    }

    private static ArrayToken ToArrayToken(double[] values)
    {
        var tokens = new IToken[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            tokens[i] = new NumericToken(values[i]);
        }
        return new ArrayToken(tokens);
    }
}
