using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.XOrder.Core
{
    public static class ModuleConstants
    {
        public static class Settings
        {
            public static class General
            {
                public const string ShippingAddressPolicyDisabled = "Disabled";
                public const string ShippingAddressPolicyPreviousOrder = "Previous Order Address";

                public static SettingDescriptor ShippingAddressPolicy { get; } = new SettingDescriptor
                {
                    Name = "XOrder.ShippingAddressPolicy",
                    ValueType = SettingValueType.ShortText,
                    GroupName = "Orders|General",
                    DefaultValue = ShippingAddressPolicyDisabled,
                    AllowedValues = new[] { ShippingAddressPolicyDisabled, ShippingAddressPolicyPreviousOrder }
                };

                public static SettingDescriptor CreateAnonymousOrder { get; } = new SettingDescriptor
                {
                    Name = "XOrder.CreateAnonymousOrderEnabled",
                    ValueType = SettingValueType.Boolean,
                    GroupName = "Orders|General",
                    DefaultValue = true,
                    IsPublic = true,
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ShippingAddressPolicy;
                        yield return CreateAnonymousOrder;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> StoreLevelSettings
            {
                get
                {
                    yield return General.ShippingAddressPolicy;
                    yield return General.CreateAnonymousOrder;
                }
            }
        }
    }
}
