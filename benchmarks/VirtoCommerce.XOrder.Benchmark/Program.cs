using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// BenchmarkSwitcher forwards CLI args (--filter, --job, --anyCategories, ...) to BenchmarkDotNet.
// Always pass --filter when running non-interactively to avoid the interactive selection prompt.
//
// Opt-in before/after comparison: `--baseline-src <path-to-src>` adds a second BenchmarkDotNet job
// ("before") that rebuilds XOrder.Core/Data from <path> (a git worktree on the baseline revision)
// and runs it against the current source ("after"), yielding a Ratio column in a single process.
// The flag is parsed out here and never reaches BDN. When absent the run is unchanged.
// See README "Comparing before/after a change".
var (baselineSrc, rest) = ExtractOption(args, "--baseline-src");

if (baselineSrc is null)
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(rest);
}
else
{
    // before+after differ ONLY by source, so the job is chosen here — extracted from args, NOT left for
    // the switcher's --job (that would append a third, unpaired job). Default to Dry: allocations are
    // deterministic at any job, so a Dry before/after yields a byte-exact Alloc Ratio in seconds (the
    // cheap routine check; the alloc axis is the trustworthy one). The time Ratio at Dry/Short is NOT a
    // verdict (cold JIT / too few iterations) — pass `--job Default` only when a trustworthy Mean is the point.
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

// Removes "<name> <value>" from args and returns the value (null if the flag is absent), so the
// remaining args pass through to BenchmarkSwitcher untouched.
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
