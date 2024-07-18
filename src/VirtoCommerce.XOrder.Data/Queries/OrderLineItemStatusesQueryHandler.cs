using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.XOrder.Core.Queries;
using OrderSettings = VirtoCommerce.OrdersModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Data.Queries;

public class OrderLineItemStatusesQueryHandler : LocalizedSettingQueryHandler<OrderLineItemStatusesQuery>
{
    public OrderLineItemStatusesQueryHandler(ILocalizableSettingService localizableSettingService)
        : base(localizableSettingService)
    {
    }

    protected override SettingDescriptor Setting => OrderSettings.OrderLineItemStatuses;
}
