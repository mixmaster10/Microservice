using ClassifiedAds.Infrastructure.Web.Authorization.Policies;
using ClassifiedAds.Services.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace ClassifiedAds.Services.Identity.Authorization.Policies.Users
{
    public class SetPasswordPolicy : IPolicy
    {
        public void Configure(AuthorizationPolicyBuilder policy)
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement
            {
                Feature = "UsersManagement",
                Action = "Set",
            });
        }
    }
}
