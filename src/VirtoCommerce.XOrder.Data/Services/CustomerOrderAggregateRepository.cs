using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XOrder.Core;
using VirtoCommerce.XOrder.Core.Services;

namespace VirtoCommerce.XOrder.Data.Services
{
    public class CustomerOrderAggregateRepository : ICustomerOrderAggregateRepository
    {
        private const string _attachmentsUrlPrefix = "/api/files/";

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

            await UpdateConfigurationFiles(order.Items);

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

        protected virtual async Task UpdateConfigurationFiles(ICollection<OrdersModule.Core.Model.LineItem> configuredItems)
        {
            var configurationItems = configuredItems
                .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                .SelectMany(x => x.ConfigurationItems.Where(y => y.Files != null))
                .ToList();

            var fileUrls = configurationItems
                .SelectMany(y => y.Files)
                .Where(x => !string.IsNullOrEmpty(x.Url))
                .Select(x => x.Url)
                .Distinct()
                .ToList();

            var ids = fileUrls
                .Select(GetFileId)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var files = await _fileUploadService.GetAsync(ids);

            files = files
                .Where(x => x.Scope == CatalogModule.Core.ModuleConstants.ConfigurationSectionFilesScope && (!string.IsNullOrEmpty(x.OwnerEntityId) || !string.IsNullOrEmpty(x.OwnerEntityType)))
                .ToList();

            if (!files.IsNullOrEmpty())
            {
                foreach (var file in files)
                {
                    var configurationItem = configurationItems.FirstOrDefault(x => x.Files.Any(y => y.Url == GetFileUrl(file.Id)));
                    file.OwnerEntityId = configurationItem?.Id;
                    file.OwnerEntityType = nameof(OrdersModule.Core.Model.ConfigurationItem);
                }

                await _fileUploadService.SaveChangesAsync(files);
            }
        }

        private static string GetFileId(string url)
        {
            return url != null && url.StartsWith(_attachmentsUrlPrefix)
                ? url[_attachmentsUrlPrefix.Length..]
                : null;
        }

        private static string GetFileUrl(string id)
        {
            return $"{_attachmentsUrlPrefix}{id}";
        }
    }
}
