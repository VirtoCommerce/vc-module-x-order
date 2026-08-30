using System.Reflection;
using VirtoCommerce.XCart.Benchmark;

// Stock (un-extended XOrder) runner. The entry-point plumbing — the --baseline-src before/after pairing
// and the --job it consumes for both halves — lives in BenchmarkProgram, which arrives transitively with
// the XCart.Benchmark.Core package that XOrder.Benchmark.Core references. It used to be copied here, and
// the copy had silently fallen behind; a shared entry point cannot.
// No [assembly: BenchmarkSetup] here: this runner writes its concrete benchmark subclass by hand
// (CreateOrderFromCartBenchmarks) rather than source-generating it, and Run only needs the assembly.
BenchmarkProgram.Run(Assembly.GetExecutingAssembly(), args);
