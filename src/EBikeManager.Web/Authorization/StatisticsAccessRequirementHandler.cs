using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;
using EBikeManager.Web.Services;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Authorization;

namespace EBikeManager.Web.Authorization;

public sealed class StatisticsAccessRequirementHandler : AuthorizationHandler<StatisticsAccessRequirement>
{
    private readonly AuthSettingsService _auth;

    public StatisticsAccessRequirementHandler(AuthSettingsService auth)
    {
        _auth = auth;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StatisticsAccessRequirement requirement)
    {
        var bearer = context.Resource is HttpContext http ? BearerToken.FromRequest(http.Request) : null;
        if (AccessPolicy.Decide(_auth.IsActive, context.User, _auth.Stamp) == Access.Full || ApiToken.Matches(bearer, _auth.Current.ApiTokenHash))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
