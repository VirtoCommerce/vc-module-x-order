# VirtoCommerce.XOrder Benchmarks

Microbenchmark for the order-creation **XAPI command handler** — the operation a developer ships
(the `createOrderFromCart` GraphQL mutation) and its real internal compute, with all I/O mocked at
the leaves. A tool for **local development and code analysis**: run it while changing order/cart
code to see the allocation and throughput effect.

The metric to trust is **allocations** (`[MemoryDiagnoser]`) — deterministic across machines and
runs. Wall-clock `Mean` is a complementary signal, meaningful only within a single controlled run.

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
graph — run each runner into separate `--artifacts` and diff the `Allocated` column (deterministic;
`Mean` from a short run is noise).

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
   diff the `Allocated` columns — reliable since allocations are deterministic across runs.
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
   the benchmarked public API stable. Do **not** add `--job <preset>` — BDN appends it as a *third*
   job rather than reconfiguring the before/after pair; for a stricter `Mean` add `--apples
   --iterationCount N`.
