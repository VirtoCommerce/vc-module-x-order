# VirtoCommerce.XOrder Benchmarks

Microbenchmark for the order-creation **XAPI command handler** — the operation a developer ships
(the `createOrderFromCart` GraphQL mutation) and its real internal compute, with all I/O mocked at
the leaves. A tool for **local development and code analysis**: run it while changing order/cart
code to see the allocation and throughput effect.

The metric to trust is **allocations** (`[MemoryDiagnoser]`) measured at **`--job Short` or above** —
reproducible across machines and runs there, and not at `--job Dry`; see
[Reading allocations: use `--job Short`](#reading-allocations-use---job-short). Wall-clock `Mean` is a
complementary signal, meaningful only within a single controlled run.

## Subject

| Benchmark | Subject | Harness |
|---|---|---|
| `CreateOrderFromCartBenchmarks` | `createOrderFromCart` → `CreateOrderFromCartCommandHandler.Handle` (flat + configured shapes × cart size) | command-level; real `CustomerOrderBuilder`, real `CustomerOrderAggregateRepository`, real `CartAggregateRepository`, mocked I/O leaves |

The configured shape carries a configuration-item set per line item, so the configured cart→order
conversion path (`ConvertCartToOrder` mapping `ConfigurationItems`) is covered — a regression there
shows up as an allocation delta in the Configured rows.

The measured compute = cart validation + cart→order conversion + **two** cart recalculates (the
cleanup save), all real; only the DB writes (`ICustomerOrderService.SaveChangesAsync`,
`IShoppingCartService.SaveChangesAsync`) are no-op mocks, and the cart load comes from a mocked
`IMediator`. The totals calculator is the **real** `DefaultShoppingCartTotalsCalculator`.

`createOrderFromCart` mutates the cart (cleanup removes its items), so each iteration rebuilds a
fresh cart aggregate in `[IterationSetup]`, outside the measured region. That forces
`InvocationCount=1`; the `Allocated` figure stays exact, only `Mean` precision softens.

### What this does NOT measure

GraphQL request duration, real CPU%, EF query/persistence time, concurrency/caching — these are
single-threaded, in-memory measurements.

## Layout and the module-agnostic seam

The benchmark **logic** (the `[Benchmark]` method, `[Params]`, the DI host, and the seam) lives in the
`VirtoCommerce.XOrder.Benchmark.Core` **library** as the abstract `CreateOrderFromCartBenchmarksBase`,
so a consuming module can reference it and run the same benchmark under its own setup. This
project is a thin runner exe: `CreateOrderFromCartBenchmarks` is a concrete subclass that bakes the
stock `XOrderBenchmarkSetup` via `CreateSetup()`.

A setup (`IOrderBenchmarkSetup`) answers the three things that differ per module:

| Member | Upstream | A consuming module |
|---|---|---|
| `CreateCartSetup()` | the stock cart graph (`XCartBenchmarkSetup`) | its own cart setup, so the conversion runs over its real cart graph |
| `ConfigureServices(services)` | nothing — the host's base order wiring is the subject | overrides the order builder / aggregate / repository + command handler |
| `CreateCommand(cartId)` | base `CreateOrderFromCartCommand` | its overridden command type (so MediatR routes to its handler) |

The **input cart** is built by the XCart benchmark Core library's `CartBenchmarkHost` (loaded +
recalculated exactly as the cart benchmarks build it, including a consumer's real cart graph via the
`CreateCart` hook), so the order benchmark's seed cart matches the cart benchmarks' shape. Only the
order-specific wiring lives in `OrderBenchmarkHost`. Core transitively brings `XCart.Core`/`XCart.Data`
and `CartModule.Data`; `OrdersModule.Data` is order-specific.

## Comparing a consuming module against upstream

Because the benchmark logic lives in the Core library, a consuming module references the
`VirtoCommerce.XOrder.Benchmark.Core` package, implements `IOrderBenchmarkSetup` (its cart setup,
its order overrides, its command), and defines a concrete subclass of `CreateOrderFromCartBenchmarksBase`
baking that setup. The **same** createOrderFromCart benchmark then runs against the consumer's order
graph — run each runner at `--job Short` into separate `--artifacts` and diff the `Allocated` column
(reproducible at Short, not at Dry; `Mean` from a short run is noise).

## Prerequisites

- .NET 10 SDK
- The `VirtoCommerce.XCart.Benchmark.Core` package on a reachable feed. It is
  restored from a **local** feed (see the project's `nuget.config`, local-only and not committed).
  Standard publication of the Core package is a separate, later decision.

## Running

```bash
cd tests/VirtoCommerce.XOrder.Benchmark

# validate first — compiles + executes each case once, no measurement
dotnet run -c Release -- --filter "*" --job Dry

# run
dotnet run -c Release -- --filter "*" --noOverwrite > benchmark.log 2>&1
```

Results are written to `BenchmarkDotNet.Artifacts/`; read the `*-report-github.md` summary.

## Comparing before/after a change

Compare rather than reading a single run's absolute numbers — the same two approaches as the XCart
benchmark README:

1. **Two runs, git-switched.** Run into separate `--artifacts` dirs (current vs changed code) and
   diff the `Allocated` columns — run both sides at `--job Short`, where allocations reproduce across
   separate runs; they do not at `--job Dry`.
2. **Single-process side-by-side (`--baseline-src`).** Point the benchmark at a baseline checkout
   of the source for `Ratio` / `Alloc Ratio` columns in one run:
   ```bash
   git worktree add /tmp/xorder-before <baseline-ref>
   dotnet run -c Release -- --filter "*" --baseline-src /tmp/xorder-before/src
   git worktree remove /tmp/xorder-before
   ```
   `--baseline-src <path>` is opt-in and additive — without it the run is unchanged. The path is the
   `src` root of the baseline checkout; the `before` job (the baseline) rebuilds
   `XOrder.Core`/`XOrder.Data` from it via `/p:BaselineSrc=<path>` (a `ProjectReference` swap, so the
   full transitive package graph still restores — a bare DLL reference would not). An `Alloc Ratio`
   of `0.85` on an `after` row means the change allocates ~15% less. Valid only when the change keeps
   the benchmarked public API stable. **Defaults to `--job Dry`, which is the wrong job for the
   allocation axis** — pass `--job Short` explicitly for any `Alloc Ratio` you intend to act on
   ([why](#reading-allocations-use---job-short)); Dry stays useful only for confirming in seconds that
   both sides still build and run. The time `Ratio` at Dry or Short is directional only, not a verdict
   — the runner prints a reminder; pass **`--job Default` for a trustworthy `Mean`**. The chosen job is
   consumed by `--baseline-src` and applied to **both** before and after (not forwarded to BDN, so it
   won't append a third unpaired job); for a stricter `Mean` add `--apples --iterationCount N` on top of
   `--job Default`.

### Reading allocations: use `--job Short`

`--job Dry` runs one cold invocation, so the first call's JIT and static initialisation land inside
the measured region — negligible against a large cart, dominant against a small one. Measured on the
XCart suite (`AddCartItemsBenchmarks`, three runs of the same binary per job), the `Allocated` spread
across runs was **36.7 %** at Dry versus **0.15 %** at Short for the one-line-item case, converging to
under 1 % on both by 100 line items. Dry is biased as well as noisy: that case reads 92–127 KB at Dry
against a steady 58.3 KB at Short.

Read allocations at `--job Short` or above, and treat a Short-to-Short difference under ~1 % as noise.
Reserve Dry for "does it still run" — it executes every case once in seconds, which is the cheapest
way to catch a broken fixture or a missing DI registration.

### Recognising a run that measured nothing

BenchmarkDotNet exits **0** whether or not a single case produced a figure, and `executed benchmarks: N`
counts attempts rather than results — so a suite whose subject cannot be constructed looks, by exit code
and summary line, exactly like a healthy one. Read these instead:

- `NA` in the **`Mean`** column. `Error` is always `NA` in a Dry run, so `Mean` is the column that tells
  a result from a failure.
- `There are not any results runs`, once per failed case, and a `Benchmarks with issues:` block listing
  them by name.
- Exceptions in `BenchmarkDotNet.Artifacts/BenchmarkRun-*.log` — they sit in the body of that file, not
  in the console tail.

### Before you trust a green result, show the arm can go red

A benchmark has no assertions, so "no regression" and "this run never touched your change" print the
same number. Two of the ways to miss are structural rather than accidental: the DB writes and the
cart load are mocked, so a change that adds a persistence round-trip or another cart fetch shows
nothing; and this project holds a single benchmark, `CreateOrderFromCart`, so anything outside the
cart→order conversion path it drives is simply not on the graph.

Before reading a `1.00` ratio as good news, perturb the code you changed — revert the optimisation,
or add an obvious allocation — and confirm the number moves. If it does not, the benchmark does not
reach your change and the result says nothing about it.
