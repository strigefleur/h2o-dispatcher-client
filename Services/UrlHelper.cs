namespace Felweed.Services;

public static class UrlHelper
{
    public static Uri? GetSafeUrl(string? urlWannabe)
    {
        if (urlWannabe is null)
            return null;

        if (!Uri.TryCreate(urlWannabe, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return null;
        
        return uriResult;
    }
}