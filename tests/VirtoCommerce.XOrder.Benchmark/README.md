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

## Prerequisites

- .NET 10 SDK

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
benchmark README: (1) two runs into separate `--artifacts` dirs, git-switched, diffing the
`Allocated` columns (reliable since allocations are deterministic); or (2) a single-process
side-by-side with a saved baseline build for a `Ratio` column (valid when the change keeps the
benchmarked public API stable).
