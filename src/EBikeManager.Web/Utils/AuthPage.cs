using System.Text.Encodings.Web;
using EBikeManager.Application.Utils;
using EBikeManager.Web.Resources;

namespace EBikeManager.Web.Utils;

public static class AuthPage
{
    public static string Render(string title, string message, string? note, string actionHref, string actionText)
    {
        var html = HtmlEncoder.Default;
        var noteHtml = note == null ? "" : $"""<p class="em-auth-note">{html.Encode(note)}</p>""";
        return $$"""
            <!DOCTYPE html>
            <html lang="{{html.Encode(WebStrings.HtmlLanguage)}}">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>{{html.Encode(ProjectInfo.Name)}} - {{html.Encode(title)}}</title>
            <link rel="icon" type="image/svg+xml" href="/img/logo.svg" />
            <link href="/css/fonts.css" rel="stylesheet" />
            <link href="/css/ebike-manager.css" rel="stylesheet" />
            </head>
            <body class="em-auth-page">
            <main class="em-auth-card">
            <img src="/img/logo.svg" alt="" class="em-auth-logo" />
            <h1>{{html.Encode(title)}}</h1>
            <p>{{html.Encode(message)}}</p>
            {{noteHtml}}
            <a href="{{html.Encode(actionHref)}}" class="em-auth-action">{{html.Encode(actionText)}}</a>
            </main>
            </body>
            </html>
            """;
    }
}
