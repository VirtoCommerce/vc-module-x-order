# Virto Commerce Order Experience API (xOrder) Module

[![CI status](https://github.com/VirtoCommerce/vc-module-x-order/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-x-order/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-order&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-order) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-order&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-order) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-order&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-order) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-order&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-order)

The xOrder module provides high-performance API for order data with the following key features:
* Getting and searching orders.
* Basic order workflow operations.
* Pluggable order product resolution via `IOrderProductResolver` and the `ExternalOrderProducts` pipeline.

## Order Product Resolution

When GraphQL clients request product data for an order's line items, xOrder routes the load through `IOrderProductResolver` — a scoped service introduced in VCST-4768 that batches, caches, and delegates product resolution for each GraphQL request.

### Resolution flow

Every order-scoped product lookup runs in two stages:

1. **`ExternalOrderProducts` pipeline** — a [PipelineNet](https://github.com/ipvalverde/PipelineNet) pipeline registered empty by default. Downstream modules add middleware to contribute products (for example, [Product Snapshot](https://github.com/VirtoCommerce/vc-module-product-snapshot) adds `LoadorderProductSnapshotMiddleware` to return frozen order-time snapshots).
2. **Catalog fallback (`OrderProductResolver.LoadProductsAsync`)** — any products the pipeline did not provide are loaded via `LoadProductsQuery` against XCatalog. `OrderProductResolveContext.IncludeFields` is populated from `IResolveFieldContext.SubFields`, so the catalog query fetches exactly the fields the GraphQL client asked for. When no dynamic fields are supplied, the resolver falls back to `DefaultIncludeFields` (`properties`, `images`, `descriptions`).

### Per-request caching

`OrderProductResolver` is registered with `AddScoped`, so each GraphQL request gets a fresh instance with its own `(orderId, productId)` cache. Duplicate resolutions within one request hit the cache; the cache is discarded with the DI scope when the request ends — there is no TTL or size cap.

### Extension points

Two common ways to plug in:

**Add middleware to the pipeline** — the low-friction option, ideal when you want to enrich or short-circuit product loading:

```csharp
serviceCollection.AddPipeline<ExternalOrderProducts>(builder =>
{
    builder.AddMiddleware(typeof(MyCustomOrderProductMiddleware));
});
```

**Replace the resolver** — subclass `OrderProductResolver` (override `LoadProductsAsync` for a different fallback source) and re-register:

```csharp
services.AddScoped<IOrderProductResolver, MyOrderProductResolver>();
```

See [Product Snapshot](https://github.com/VirtoCommerce/vc-module-product-snapshot) for a full example of a module that plugs into the `ExternalOrderProducts` pipeline.

## Documentation

* [xOrder module documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/Order/overview/)
* [Experience API Documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/)
* [Getting started](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/getting-started/)
* [How to use GraphiQL](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/graphiql/)
* [How to use Postman](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/postman/)
* [How to extend](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/x-api-extensions/)
* [Virto Commerce Frontend architecture](https://docs.virtocommerce.org/storefront/developer-guide/architecture/)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-x-order)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-x-order/releases/latest)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
