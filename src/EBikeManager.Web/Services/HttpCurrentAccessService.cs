using EBikeManager.Application.Enums;
using EBikeManager.Web.Utils;

namespace EBikeManager.Web.Services;

public sealed class HttpCurrentAccessService
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly AuthSettingsService _auth;

    public HttpCurrentAccessService(IHttpContextAccessor httpContext, AuthSettingsService auth)
    {
        _httpContext = httpContext;
        _auth = auth;
    }

    public Access Level => _httpContext.HttpContext is { } context ? AccessPolicy.Decide(_auth.IsActive, context.User, _auth.Stamp) : Access.None;
}
