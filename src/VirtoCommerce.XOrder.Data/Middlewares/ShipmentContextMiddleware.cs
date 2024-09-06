using System;
using System.Linq;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.XCart.Core.Models;
using static VirtoCommerce.CoreModule.Core.Common.AddressType;
using XOrderSetting = VirtoCommerce.XOrder.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Data.Middlewares
{
    public class ShipmentContextMiddleware : IAsyncMiddleware<ShipmentContextCartMap>
    {
        private readonly ICustomerOrderSearchService _customerOrderSearchService;

        public ShipmentContextMiddleware(ICustomerOrderSearchService customerOrderSearchService)
        {
            _customerOrderSearchService = customerOrderSearchService;
        }

        public async Task Run(ShipmentContextCartMap parameter, Func<ShipmentContextCartMap, Task> next)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            var shipment = parameter.Shipment;
            var shippingAddressPolicy = GetShippingPolicy(parameter);

            if (shipment?.DeliveryAddress != null || !shippingAddressPolicy.EqualsInvariant(XOrderSetting.ShippingAddressPolicyPreviousOrder))
            {
                return;
            }

            var lastOrderCriteria = new CustomerOrderSearchCriteria
            {
                CustomerId = parameter.CartAggregate.Cart.CustomerId,
                Take = 1
            };

            var lastOrderResult = await _customerOrderSearchService.SearchNoCloneAsync(lastOrderCriteria);

            if (lastOrderResult.Results.Count != 0)
            {
                var order = lastOrderResult.Results[0];
                var address = order.Addresses?.FirstOrDefault(x => x.AddressType == BillingAndShipping || x.AddressType == Shipping);

                if (address != null)
                {
                    var cartShipmentAddress = CreateCartShipmentAddress(address);
                    parameter.Shipment.DeliveryAddress = cartShipmentAddress;
                }
            }
        }

        private static string GetShippingPolicy(ShipmentContextCartMap parameter)
        {
            return parameter.CartAggregate?.Store?.Settings?.GetValue<string>(XOrderSetting.ShippingAddressPolicy);
        }

        protected virtual Address CreateCartShipmentAddress(OrdersModule.Core.Model.Address address)
        {
            var cartShipmentAddress = AbstractTypeFactory<Address>.TryCreateInstance();

            cartShipmentAddress.Key = null;
            cartShipmentAddress.Name = address.Name;
            cartShipmentAddress.City = address.City;
            cartShipmentAddress.CountryCode = address.CountryCode;
            cartShipmentAddress.CountryName = address.CountryName;
            cartShipmentAddress.Phone = address.Phone;
            cartShipmentAddress.PostalCode = address.PostalCode;
            cartShipmentAddress.RegionId = address.RegionId;
            cartShipmentAddress.RegionName = address.RegionName;
            cartShipmentAddress.City = address.City;
            cartShipmentAddress.Email = address.Email;
            cartShipmentAddress.FirstName = address.FirstName;
            cartShipmentAddress.LastName = address.LastName;
            cartShipmentAddress.Line1 = address.Line1;
            cartShipmentAddress.Line2 = address.Line2;
            cartShipmentAddress.AddressType = address.AddressType;
            cartShipmentAddress.Organization = address.Organization;
            cartShipmentAddress.OuterId = address.OuterId;
            cartShipmentAddress.Description = address.Description;

            return cartShipmentAddress;
        }
    }
}
