using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.XOrder.Core.Queries;
using OrderSettings = VirtoCommerce.OrdersModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XOrder.Data.Queries;

public class PaymentStatusesQueryHandler : LocalizedSettingQueryHandler<PaymentStatusesQuery>
{
    public PaymentStatusesQueryHandler(ILocalizableSettingService localizableSettingService)
        : base(localizableSettingService)
    {
    }

    protected override SettingDescriptor Setting => OrderSettings.PaymentInStatus;
}
