using System;
using GraphQL.DI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Services;
using VirtoCommerce.XOrder.Data.Authorization;
using VirtoCommerce.XOrder.Data.Middlewares;
using VirtoCommerce.XOrder.Data.Services;

namespace VirtoCommerce.XOrder.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXOrder(this IServiceCollection services, IGraphQLBuilder graphQLBuilder)
        {
            // disable scoped schema for now
            //services.AddSingleton<ScopedSchemaFactory<DataAssemblyMarker>>();

            services.AddTransient<ICustomerOrderAggregateRepository, CustomerOrderAggregateRepository>();
            services.AddSingleton<IAuthorizationHandler, CanAccessOrderAuthorizationHandler>();

            services.AddTransient<CustomerOrderAggregate>();
            services.AddTransient<Func<CustomerOrderAggregate>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<CustomerOrderAggregate>());

            services.AddPipeline<PromotionEvaluationContextCartMap>(builder =>
            {
                builder.AddMiddleware(typeof(EvalPromoContextOrderMiddleware));
            });

            services.AddPipeline<ShipmentContextCartMap>(builder =>
            {
                builder.AddMiddleware(typeof(ShipmentContextMiddleware));
            });

            return services;
        }
    }
}
