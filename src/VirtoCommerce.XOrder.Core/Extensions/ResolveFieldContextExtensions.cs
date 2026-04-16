using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XOrder.Core.Models;

namespace VirtoCommerce.XOrder.Core.Extensions
{
    public static class ResolveFieldContextExtensions
    {
        extension(IResolveFieldContext userContext)
        {
            public CustomerOrderAggregate GetOrder()
            {
                return userContext.GetValueForSource<CustomerOrderAggregate>();
            }

            public OrderProductResolveContext CreateOrderProductResolveContext(CustomerOrderAggregate orderAggregate)
            {
                var order = orderAggregate.Order;
                var resolveContext = AbstractTypeFactory<OrderProductResolveContext>.TryCreateInstance();
                resolveContext.UserId = userContext.GetArgumentOrValue<string>("userId") ?? userContext.GetCurrentUserId();
                resolveContext.OrganizationId = userContext.GetCurrentOrganizationId();
                resolveContext.StoreId = order.StoreId;
                resolveContext.CurrencyCode = order.Currency;
                resolveContext.CultureName = userContext.GetArgumentOrValue<string>("cultureName") ?? order.LanguageCode;

                return resolveContext;
            }

            public T ExtractQuery<T>() where T : IExtendableQuery
            {
                var query = AbstractTypeFactory<T>.TryCreateInstance();
                query.Map(userContext);

                return query;
            }
        }

        extension<T>(IResolveFieldContext<T> userContext)
        {
            public Currency GetOrderCurrency()
            {
                return userContext.GetOrder()?.Currency;
            }

            public Currency GetOrderCurrencyByCode(string currencyCode)
            {
                //Try to get a currency from order if currency code is not set explicitly or undefined
                var result = userContext.GetOrderCurrency();

                //If the passed currency differs from the order currency, we try to find it from the all registered currencies.
                if (result == null || !string.IsNullOrEmpty(currencyCode) && !result.Code.EqualsIgnoreCase(currencyCode))
                {
                    var allCurrencies = userContext.GetValue<IEnumerable<Currency>>("allCurrencies");
                    result = allCurrencies?.FirstOrDefault(x => x.Code.EqualsIgnoreCase(currencyCode));
                }

                return result ?? throw new OperationCanceledException($"the currency with code '{currencyCode}' is not registered");
            }
        }
    }
}
