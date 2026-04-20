using System.Text;
using Microsoft.Extensions.Logging;

namespace DevToys.Base64Url;

internal enum Base64Encoding
{
    Utf8,
    Ascii
}

internal static class Base64UrlHelper
{
    internal static bool IsBase64UrlDataStrict(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        // Check for padding '=' which shouldn't exist in strict Base64Url
        if (data.Contains('='))
        {
            return false;
        }

        // Check for standard Base64 characters that shouldn't be in Base64Url
        if (data.Contains('+') || data.Contains('/'))
        {
            return false;
        }

        var normalized = data
            .Replace('-', '+')
            .Replace('_', '/');
        
        switch (normalized.Length % 4)
        {
            case 2: normalized += "=="; break;
            case 3: normalized += "="; break;
        }

        Span<byte> buffer = new byte[normalized.Length];
        return Convert.TryFromBase64String(normalized, buffer, out _);
    }

    internal static string FromTextToBase64Url(string input, Base64Encoding encoding, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        try
        {
            var bytes = encoding switch
            {
                Base64Encoding.Ascii => Encoding.ASCII.GetBytes(input),
                Base64Encoding.Utf8 => Encoding.UTF8.GetBytes(input),
                _ => throw new NotSupportedException()
            };

            cancellationToken.ThrowIfCancellationRequested();

            var base64 = Convert.ToBase64String(bytes);

            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to encode to Base64Url.");
            return string.Empty;
        }
    }

    internal static string FromBase64UrlToText(string input, Base64Encoding encoding, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        try
        {
            var normalized = input
                .Replace('-', '+')
                .Replace('_', '/');
            
            switch (normalized.Length % 4)
            {
                case 2: normalized += "=="; break;
                case 3: normalized += "="; break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var bytes = Convert.FromBase64String(normalized);

            return encoding switch
            {
                Base64Encoding.Ascii => Encoding.ASCII.GetString(bytes),
                Base64Encoding.Utf8 => Encoding.UTF8.GetString(bytes),
                _ => throw new NotSupportedException()
            };
        }
        catch (FormatException)
        {
            return Base64Url.InvalidBase64Url;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decode Base64Url.");
            return string.Empty;
        }
    }
}
