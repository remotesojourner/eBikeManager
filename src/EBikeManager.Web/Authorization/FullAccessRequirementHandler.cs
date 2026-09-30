using EBikeManager.Application.Enums;
using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authorization;

namespace EBikeManager.Web.Authorization;

public sealed class FullAccessRequirementHandler : AuthorizationHandler<FullAccessRequirement>
{
    private readonly AuthSettingsService _auth;

    public FullAccessRequirementHandler(AuthSettingsService auth)
    {
        _auth = auth;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FullAccessRequirement requirement)
    {
        if (AccessPolicy.Decide(_auth.IsActive, context.User, _auth.Stamp) == Access.Full) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
