using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

if (baselineSrc is null)
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(rest);
}
else
{
    // before+after differ ONLY by source, so the job is chosen here — extracted from args, NOT left for
    // the switcher's --job (that would append a third, unpaired job). The Dry default confirms in seconds
    // that both sides still build and run; it is the wrong job for the allocation axis — see the README
    // section "Reading allocations: use --job Short".
    var (jobName, restAfterJob) = ExtractOption(rest, "--job");
    rest = restAfterJob;
    var normalized = (jobName ?? "dry").ToLowerInvariant();
    var baselineJob = normalized switch
    {
        "dry" => Job.Dry,
        "short" => Job.ShortRun,
        "default" or "measured" => Job.Default,
        _ => throw new ArgumentException($"--job must be Dry|Short|Default with --baseline-src; got '{jobName}'."),
    };
    if (normalized is not ("default" or "measured"))
    {
        Console.Error.WriteLine($"// --baseline-src on --job {normalized}: Alloc Ratio is exact; the time " +
            "Ratio is directional only (not a verdict) — re-run with `--job Default` for a trustworthy Mean.");
    }

    var config = ManualConfig.Create(DefaultConfig.Instance)
        .AddJob(baselineJob.WithMsBuildArguments($"/p:BaselineSrc={baselineSrc}").WithId("before").AsBaseline())
        .AddJob(baselineJob.WithId("after"));

    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(rest, config);
}

static (string, string[]) ExtractOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    if (index < 0)
    {
        return (null, args);
    }

    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"{name} requires a path argument.");
    }

    var value = args[index + 1];
    var rest = args.Where((_, i) => i != index && i != index + 1).ToArray();

    return (value, rest);
}
