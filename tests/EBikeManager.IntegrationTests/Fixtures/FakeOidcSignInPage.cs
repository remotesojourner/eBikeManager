using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;

namespace EBikeManager.IntegrationTests.Fixtures;

public sealed class FakeOidcSignInPage(FakeOidcProvider oidc) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, following) =>
        {
            if (context.Request.Path == FakeOidcProvider.AuthorizePath)
            {
                context.Response.Redirect(oidc.Approve(new Uri(context.Request.GetEncodedUrl())).AbsoluteUri);
                return;
            }

            await following(context);
        });
        next(app);
    };
}
