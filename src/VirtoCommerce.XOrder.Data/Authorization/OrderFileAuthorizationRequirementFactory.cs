using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Authorization;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XOrder.Data.Authorization;

public class OrderFileAuthorizationRequirementFactory : IFileAuthorizationRequirementFactory
{
    public bool CanCreateRequirement(File file)
    {
        return file.Scope.EqualsIgnoreCase(ConfigurationSectionFilesScope) && file.OwnerTypeIs<CustomerOrder>();
    }

    public IAuthorizationRequirement Create(File file, string permission)
    {
        return new CanAccessOrderAuthorizationRequirement();
    }
}
