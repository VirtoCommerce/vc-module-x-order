using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XOrder.Data.Services
{
    public class CustomerOrderAggregateRepository : ICustomerOrderAggregateRepository
    {
        private readonly Func<CustomerOrderAggregate> _customerOrderAggregateFactory;
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerOrderBuilder _customerOrderBuilder;
        private readonly IFileUploadService _fileUploadService;

        public CustomerOrderAggregateRepository(
            Func<CustomerOrderAggregate> customerOrderAggregateFactory,
            ICustomerOrderService customerOrderService,
            ICurrencyService currencyService,
            ICustomerOrderBuilder customerOrderBuilder,
            IFileUploadService fileUploadService)
        {
            _customerOrderAggregateFactory = customerOrderAggregateFactory;
            _customerOrderService = customerOrderService;
            _currencyService = currencyService;
            _customerOrderBuilder = customerOrderBuilder;
            _fileUploadService = fileUploadService;
        }

        public async Task<CustomerOrderAggregate> GetOrderByIdAsync(string orderId)
        {
            var order = await _customerOrderService.GetByIdAsync(orderId);
            if (order != null)
            {
                var result = await InnerGetCustomerOrderAggregatesFromCustomerOrdersAsync([order]);
                return result.FirstOrDefault();
            }
            return null;
        }

        public virtual async Task<CustomerOrderAggregate> CreateOrderFromCart(ShoppingCart cart)
        {
            var order = await _customerOrderBuilder.PlaceCustomerOrderFromCartAsync(cart);
            await UpdateConfigurationFiles(order);
            var aggregates = await InnerGetCustomerOrderAggregatesFromCustomerOrdersAsync([order], order.LanguageCode);

            return aggregates.FirstOrDefault();
        }

        public async Task<CustomerOrderAggregate> GetAggregateFromOrderAsync(CustomerOrder order)
        {
            var result = await InnerGetCustomerOrderAggregatesFromCustomerOrdersAsync([order]);
            return result.FirstOrDefault();
        }

        public Task<IList<CustomerOrderAggregate>> GetAggregatesFromOrdersAsync(IList<CustomerOrder> orders, string cultureName = null)
        {
            return InnerGetCustomerOrderAggregatesFromCustomerOrdersAsync(orders, cultureName);
        }

        protected virtual async Task<IList<CustomerOrderAggregate>> InnerGetCustomerOrderAggregatesFromCustomerOrdersAsync(IList<CustomerOrder> orders, string cultureName = null)
        {
            var currencies = await _currencyService.GetAllCurrenciesAsync();

            return orders.Select(x =>
            {
                var aggregate = _customerOrderAggregateFactory();
                aggregate.GrabCustomerOrder(x.CloneTyped(), currencies.GetCurrencyForLanguage(x.Currency, cultureName ?? x.LanguageCode));
                return aggregate;
            }).ToList();
        }

        private async Task UpdateConfigurationFiles(CustomerOrder order)
        {
            var configurationItems = order.Items
                .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                .SelectMany(x => x.ConfigurationItems.Where(y => y.Files != null))
                .ToList();

            var fileUrls = configurationItems
                .SelectMany(x => x.Files)
                .Where(x => !string.IsNullOrEmpty(x.Url))
                .Select(x => x.Url)
                .Distinct()
                .ToArray();

            var files = (await _fileUploadService.GetByPublicUrlAsync(fileUrls))
                .Where(x => x.Scope == ConfigurationSectionFilesScope)
                .ToList();

            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    var configurationItem = configurationItems.FirstOrDefault(i => i.Files.Any(f => f.Url == file.PublicUrl));
                    if (configurationItem != null)
                    {
                        file.SetOwner(configurationItem);
                    }
                }

                await _fileUploadService.SaveChangesAsync(files);
            }
        }
    }
}
