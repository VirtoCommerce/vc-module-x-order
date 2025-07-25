using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XOrder.Core.Extensions
{
    public static class ResolveFieldContextExtensions
    {
        public static Currency GetOrderCurrency<T>(this IResolveFieldContext<T> userContext)
        {
            return userContext.GetValueForSource<CustomerOrderAggregate>()?.Currency;
        }

        public static Currency GetOrderCurrencyByCode<T>(this IResolveFieldContext<T> userContext, string currencyCode)
        {
            //Try to get a currency from order if currency code is not set explicitly or undefined
            var result = userContext.GetOrderCurrency();
            //If the passed currency differs from the order currency, we try to find it from the all registered currencies.
            if (result == null || !string.IsNullOrEmpty(currencyCode) && !result.Code.EqualsIgnoreCase(currencyCode))
            {
                var allCurrencies = userContext.GetValue<IEnumerable<Currency>>("allCurrencies");
                result = allCurrencies?.FirstOrDefault(x => x.Code.EqualsIgnoreCase(currencyCode));
            }
            if (result == null)
            {
                throw new OperationCanceledException($"the currency with code '{currencyCode}' is not registered");
            }
            return result;
        }

        public static T ExtractQuery<T>(this IResolveFieldContext context) where T : IExtendableQuery
        {
            var query = AbstractTypeFactory<T>.TryCreateInstance();
            query.Map(context);
            return query;
        }
    }
}
