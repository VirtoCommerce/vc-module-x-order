# VirtoCommerce.XOrder Benchmarks

L1 microbenchmark for the order-creation **XAPI command handler** — the operation a developer
ships (the `createOrderFromCart` GraphQL mutation) and its real internal compute, with all I/O
mocked at the leaves.

A **regression coverage suite**: the gating metric is **allocations** (`[MemoryDiagnoser]`,
deterministic across machines). Wall-clock `Mean` is informative but not portable.

## Subject

| Benchmark | Subject | Harness |
|---|---|---|
| `CreateOrderFromCartBenchmarks` | `createOrderFromCart` → `CreateOrderFromCartCommandHandler.Handle` | command-level; real `CustomerOrderBuilder`, real `CustomerOrderAggregateRepository`, real `CartAggregateRepository`, mocked I/O leaves |

The operation's real compute = cart validation + cart→order conversion + **two** cart recalculates
(the cleanup save), all measured; only the DB writes (`ICustomerOrderService.SaveChangesAsync`,
`IShoppingCartService.SaveChangesAsync`) are no-op mocks, and the cart load is supplied by a mocked
`IMediator`. The totals calculator is the **real** `DefaultShoppingCartTotalsCalculator`.

`createOrderFromCart` mutates the cart (cleanup removes items), so each iteration rebuilds a fresh
cart aggregate in `[IterationSetup]` — outside the measured region. That forces `InvocationCount=1`;
the `Allocated` gate stays exact, only `Mean` precision softens (it is the secondary metric).

### What L1 does NOT measure

GraphQL request duration, real CPU%, EF query/persistence time, concurrency/caching. Those are
L2/L3.

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

## Detecting a regression (before/after)

Compare side-by-side rather than reading a single run's absolute numbers — see the XCart benchmark
README for the saved-DLL before/after workflow (BenchmarkDotNet comparison strategy 4). Results are
written to `BenchmarkDotNet.Artifacts/`; read the `*-report-github.md` summary.
